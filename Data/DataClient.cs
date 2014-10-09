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
            JObject json = await GetFile(entity);

            return await FindEntityInJson(entity, json);
        }

        private async Task<JToken> FindEntityInJson(Uri entity, JObject json)
        {
            string search = entity.AbsoluteUri;

            var idNode = json.Descendants().Where(n =>
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
                return idNode.Parent;
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
        public async Task<JToken> Ensure(JToken jToken, IEnumerable<Uri> properties)
        {
            return jToken;
        }

        private async static Task<JObject> StreamToJson(Stream stream)
        {
            JObject jObj = null;

            if (stream != null)
            {
                using (var reader = new StreamReader(stream))
                {
                    string json = reader.ReadToEnd();
                    jObj = JObject.Parse(json);
                }
            }

            return jObj;
        }
    }
}
