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
        private static readonly TimeSpan _lifeSpan = TimeSpan.FromMinutes(5);

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
        public async Task<JObject> GetFile(Uri uri, bool cache=true)
        {
            Uri fixedUri = uri;

            if (uri.AbsoluteUri.IndexOf('#') > -1)
            {
                fixedUri = new Uri(uri.AbsoluteUri.Split('#')[0]);
            }

            Stream stream = null;
            JObject result = null;

            try
            {
                using (var uriLock = new UriLock(fixedUri))
                {
                    if (!cache || !_fileCache.TryGet(fixedUri, out stream))
                    {
                        // the stream was not in the cache or we are skipping the cache
                        int tries = 0;

                        // try up to 5 times to be a little more robust
                        while (stream == null && tries < 5)
                        {
                            tries++;

                            try
                            {
                                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, fixedUri.AbsoluteUri);

                                DataTraceSources.Verbose("[HttpClient] GET {0}", fixedUri.AbsoluteUri);

                                var response = await _httpClient.SendAsync(request);

                                if (response.StatusCode == HttpStatusCode.OK)
                                {
                                    if (!response.Headers.CacheControl.NoStore)
                                    {
                                        // TODO: Determine the real lifespan from the headers
                                    }

                                    stream = await response.Content.ReadAsStreamAsync();

                                    if (stream != null)
                                    {
                                        if (cache)
                                        {
                                            DataTraceSources.Verbose("[HttpClient] Caching {0}");
                                            _fileCache.Add(fixedUri, _lifeSpan, stream);
                                        }

                                        DataTraceSources.Verbose("[HttpClient] 200 OK Length: {0}", "" + stream.Length);
                                        result = await StreamToJson(stream);
                                    }
                                }
                                else
                                {
                                    DataTraceSources.Verbose("[HttpClient] FAILED {0}", "" + (int)response.StatusCode);
                                    result = new JObject();
                                    result.Add("HttpStatusCode", (int)response.StatusCode);
                                }
                            }
                            catch (HttpRequestException ex)
                            {
                                Debug.Fail("WebRequest failed: " + ex.ToString());
                                DataTraceSources.Verbose("[HttpClient] FAILED {0}", ex.ToString());

                                // request error
                                result = new JObject();
                                result.Add("HttpRequestException", ex.ToString());
                            }
                        }
                    }
                    else
                    {
                        // the stream was in the cache
                        DataTraceSources.Verbose("[HttpClient] Cached Length: {0}", "" + stream.Length);
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

            if (result != null && cache)
            {
                // this call is only blocking if the cache is overloaded
                _entityCache.Add(result, fixedUri);
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
                    DataTraceSources.Verbose("[EntityCache] Unable to get entity {0}", entity.AbsoluteUri);
                    Debug.Fail("Unable to get entity");
                }
            }

            return token;
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
            if (Utility.IsEntityFromPage(token) == false)
            {
                Uri entity = Utility.GetEntityUri(token);

                if (entity != null)
                {
                    // determine if we should fetch the page or give up
                    bool? fetch = await _entityCache.FetchNeeded(entity, properties);

                    // we are missing properties and do not have the page
                    if (fetch == true)
                    {
                        await GetFile(entity);
                    }

                    // null means either there is no work to do, or that we gave up, return the original token here
                    if (fetch != null)
                    {
                        return await _entityCache.GetEntity(entity);
                    }
                }
                else
                {
                    DataTraceSources.Verbose("[EntityCache] Unable to find entity @id!");
                }
            }
            else
            {
                DataTraceSources.Verbose("[EntityCache] Entity is from this page.");
            }

            return token;
        }

        private async static Task<JObject> StreamToJson(Stream stream)
        {
            JObject jObj = null;

            if (stream != null)
            {
                try
                {
                    stream.Seek(0, SeekOrigin.Begin);

                    using (var reader = new StreamReader(stream))
                    {
                        string json = await reader.ReadToEndAsync();
                        jObj = JObject.Parse(json);
                    }
                } 
                catch (Exception ex)
                {
                    DataTraceSources.Verbose("[StreamToJson] Failed {0}", ex.ToString());
                    Debug.Fail("Unable to parse json: " + ex.ToString());
                }
            }
            else
            {
                DataTraceSources.Verbose("[StreamToJson] Null stream!");
            }

            return jObj;
        }
    }
}
