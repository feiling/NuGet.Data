using JsonLD.Core;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Data
{
    public class DataClient
    {
        private readonly CacheHttpClient _httpClient;
        private readonly FileCacheBase _fileCache;
        private readonly EntityCache _entityCache;
        private readonly JToken _context;

        public DataClient()
            : this(null)
        {

        }

        public DataClient(JToken context)
            : this(new CacheHttpClient(), new MemoryFileCache(), context)
        {

        }

        public DataClient(CacheHttpClient httpClient, FileCacheBase fileCache, JToken context)
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
        public async Task<JToken> GetFile(Uri uri)
        {
            JObject result = null;

            using (var uriLock = new UriLock(uri))
            {
                FileCacheEntry entry = null;

                if (!_fileCache.TryGet(uri, out entry))
                {
                    result = await _httpClient.GetJObjectAsync(uri);

                    entry = new MemoryFileCacheEntry(result, uri, DateTime.UtcNow, TimeSpan.FromHours(1));
                    _fileCache.Add(entry);

                    // TODO: Move this call
                    await _entityCache.Add(result);
                }
            }

            return result;
        }

        /// <summary>
        /// Fetches the properties and returns them in the expanded form.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ensureProperties"></param>
        /// <returns></returns>
        public async Task<JToken> GetInclusiveView(Uri id, IEnumerable<Uri> ensureProperties)
        {
            var graph = await _entityCache.DescribeRecursive(id);

            var subjectGraph = graph.SelectSubject(id);

            bool missing = false;

            foreach (var prop in ensureProperties)
            {
                if (!subjectGraph.SelectPredicate(prop).Triples.Any())
                {
                    missing = true;
                    break;
                }
            }

            if (missing)
            {
                // retrieve the file
                JToken file = await GetFile(id);
                await _entityCache.Add(file);

                // try again
                graph = await _entityCache.DescribeRecursive(id);
            }


            return await CreateJson(graph, _context, id);
        }

        /// <summary>
        /// Returns all known things about the id, fetching it if needed, but making only 1 fetch max.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<JToken> GetInclusiveView(Uri id)
        {
            return await DescribeRecursive(id, _context);
        }

        /// <summary>
        /// Limited to only the critical properties.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ensureProperties"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<JToken> GetExclusiveView(Uri id, IEnumerable<Uri> criticalProperties, JToken context, bool allowFetch)
        {
            throw new NotImplementedException();
        }

        private async Task<JToken> DescribeRecursive(Uri entity, JToken context)
        {
            HashSet<Triple> triples = new HashSet<Triple>();

            var graph = await _entityCache.DescribeRecursive(entity);

            //graph.SelectSubject(entity).SelectPredicate(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#type"));

            return await CreateJson(graph, context, entity);
        }

        private async Task<JToken> CreateJson(BasicGraph graph, JToken context, Uri frameSchemaType)
        {
            var jObj = JsonLdProcessor.FromRDF(new JValue(graph.NQuads));

            //foreach (var node in graph.SelectPredicate(new Uri("http://nugetjohtaylo.blob.core.windows.net/ver3/registration/entityframework/index.json")).Triples)
            //{
            //    var x = node.Object.GetValue();
            //}

            JObject frame = new JObject();

            if (context != null)
            {
                frame["@context"] = context["@context"];
            }

            frame["@type"] = "http://schema.nuget.org/schema#CatalogRoot";

            var expanded = JsonLdProcessor.Expand(jObj, new JsonLdOptions());
            var flattened = JsonLdProcessor.Flatten(expanded, new JsonLdOptions());
            var framed = JsonLdProcessor.Frame(flattened, frame, new JsonLdOptions());
            var compacted = JsonLdProcessor.Compact(framed, context, new JsonLdOptions());

            return compacted;
        }
    }
}
