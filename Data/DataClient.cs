using JsonLD.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NuGet.Data
{
    public class DataClient
    {
        private readonly HttpClient _httpClient;
        private readonly FileCacheBase _fileCache;
        private readonly JToken _context;
        private readonly EntityCache _entityCache;

        /// <summary>
        /// DataClient with the default options.
        /// </summary>
        public DataClient()
            : this(null)
        {

        }

        /// <summary>
        /// DataClient that uses a custom context.
        /// </summary>
        /// <param name="context"></param>
        public DataClient(JToken context)
            : this(new CacheHttpClient(), new BrowserFileCache(), context)
        {

        }

        /// <summary>
        /// DataClient
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="fileCache"></param>
        /// <param name="context"></param>
        public DataClient(HttpClient httpClient, FileCacheBase fileCache, JToken context)
        {
            _httpClient = httpClient;
            _fileCache = fileCache;
            _context = context;
            _entityCache = new EntityCache();
        }

        /// <summary>
        /// Retrieves a url and returns it as it is.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public async Task<JObject> GetFile(Uri uri)
        {
            Uri fixedUri = uri;

            if (uri.AbsoluteUri.IndexOf('#') > -1)
            {
                fixedUri = new Uri(uri.AbsoluteUri.Split('#')[0]);
            }

            JObject result = null;
            Stream stream = null;

            try
            {
                using (var uriLock = new UriLock(fixedUri))
                {
                    if (!_fileCache.TryGet(fixedUri, out stream))
                    {
                        // the stream was not in the cache

                        int tries = 0;

                        // try up to 5 times to be a little more robust
                        while (stream == null && tries < 5)
                        {
                            tries++;

                            try
                            {
                                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, fixedUri.AbsoluteUri);

                                var response = await _httpClient.SendAsync(request);

                                if (response.StatusCode == HttpStatusCode.OK)
                                {
                                    TimeSpan lifeSpan = TimeSpan.FromMinutes(30);

                                    if (!response.Headers.CacheControl.NoStore)
                                    {
                                        // TODO: Determine the real lifespan from the headers
                                    }

                                    stream = await response.Content.ReadAsStreamAsync();

                                    if (stream != null)
                                    {
                                        _fileCache.Add(fixedUri, lifeSpan, stream);
                                        stream.Seek(0, SeekOrigin.Begin);

                                        result = await StreamToJson(stream);
                                    }
                                }
                                else
                                {
                                    result = new JObject();
                                    result.Add("HttpStatusCode", (int)response.StatusCode);
                                }
                            }
                            catch (HttpRequestException ex)
                            {
                                Debug.Fail("WebRequest failed: " + ex.ToString());

                                // request error
                                result = new JObject();
                                result.Add("HttpRequestException", ex.ToString());
                            }
                        }
                    }
                    else
                    {
                        // the stream was in the cache
                        result = await StreamToJson(stream);
                    }
                }
            }
            finally
            {
                if (stream != null)
                {
                    stream.Dispose();
                }
            }

            if (result != null)
            {
                // reduce and add
                _entityCache.Reduce(50000);
                await _entityCache.Add(result, fixedUri);
            }

            if (result != null)
            {
                result = result.DeepClone() as JObject;
            }

            return result;
        }

        /// <summary>
        /// Returns a JToken for the given entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="allowFetch">Allow downloading new Json.</param>
        /// <returns>The entity Json</returns>
        public async Task<JToken> GetEntity(Uri entity)
        {
            JToken token = await _entityCache.GetEntity(entity);

            if (token == null)
            {
                await GetFile(entity);
                var result = await _entityCache.GetEntity(entity);

                if (result != null)
                {
                    token = result.DeepClone();
                }
                else
                {
                    Debug.Fail("Unable to get entity");
                }
            }

            return token;
        }

        private async Task<JToken> FindEntityInJson(Uri entity, JObject json)
        {
            string search = entity.AbsoluteUri;

            var idNode = json.Descendants().Concat(json).Where(n =>
                {
                    JProperty prop = n as JProperty;

                    if (prop != null)
                    {
                        string url = prop.Value.ToString();

                        return StringComparer.Ordinal.Equals(url, search);
                    }

                    return false;
                }).FirstOrDefault();

            if (idNode != null)
            {
                return idNode.Parent.DeepClone();
            }

            return null;
        }

        /// <summary>
        /// Ensures that the given properties are on the JToken. If they are not inlined they will be fetched.
        /// Other data may appear in the returned JToken, but the root level will stay the same.
        /// </summary>
        /// <param name="jToken">The JToken to expand. This should have an @id.</param>
        /// <param name="properties">Expanded form properties that are needed on JToken.</param>
        /// <returns>The same JToken if it already exists, otherwise the fetched JToken.</returns>
        public async Task<JToken> Ensure(JToken token, IEnumerable<Uri> properties)
        {
            if (IsEntityFromPage(token) == false)
            {
                Uri entity = GetEntityUri(token);

                bool fetch = await _entityCache.FetchNeeded(entity, properties);

                if (fetch)
                {
                    await GetFile(entity);
                }

                return await _entityCache.GetEntity(entity);
            }

            return token;
        }

        private static bool? IsEntityFromPage(JToken token)
        {
            bool? result = null;
            Uri uri = GetEntityUri(token);

            if (uri != null)
            {
                var rootUri = GetEntityUri(GetRoot(token));

                result = CompareRootUris(uri, rootUri);
            }

            return result;
        }

        private static bool CompareRootUris(Uri a, Uri b)
        {
            var x = GetUriWithoutHash(a);
            var y = GetUriWithoutHash(b);

            return x.Equals(y);
        }

        private static Uri GetUriWithoutHash(Uri uri)
        {
            string s = uri.AbsoluteUri;
            int hash = s.IndexOf('#');

            if (hash > -1)
            {
                s = s.Substring(0, hash);
                return new Uri(s);
            }
            else
            {
                return uri;
            }
        }

        private static JToken GetRoot(JToken token)
        {
            JToken parent = token;

            while (parent.Parent != null)
            {
                parent = parent.Parent;
            }

            return parent;
        }

        private static Uri GetEntityUri(JToken token)
        {
            JObject jObj = token as JObject;

            if (jObj != null)
            {
                JToken urlValue;
                if (jObj.TryGetValue("url", out urlValue))
                {
                    return new Uri(urlValue.ToString());
                }
            }

            return null;
        }

        private async static Task<JObject> StreamToJson(Stream stream)
        {
            JObject jObj = null;

            if (stream != null)
            {
                try
                {
                    using (var reader = new StreamReader(stream))
                    {
                        string json = reader.ReadToEnd();
                        jObj = JObject.Parse(json);
                    }
                } 
                catch (Exception ex)
                {
                    Debug.Fail("Unable to parse json: " + ex.ToString());
                }
            }

            return jObj;
        }
    }
}
