using JsonLD.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Data
{
    public class EntityCache
    {
        private const string CacheNode = "http://nuget.org/cache/node";
        private readonly JsonLdGraphOld _masterGraph;
        private readonly Queue<JsonLdPage> _pages;

        public EntityCache()
        {
            _masterGraph = new JsonLdGraphOld();
            _pages = new Queue<JsonLdPage>();
        }

        public bool HasPageOfEntity(Uri entity)
        {
            JsonLdPage page = new JsonLdPage(GetUriWithoutHash(entity));

            lock (this)
            {
                return _pages.Contains(page);
            }
        }

        private Uri GetUriWithoutHash(Uri uri)
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

        /// <summary>
        /// True - we are missing properties and do not have the page
        /// False - we have the official page, and should turn that JToken
        /// Null - no properties are missing, and we don't have the page, but we can return the same one
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        public async Task<bool?> FetchNeeded(Uri entity, IEnumerable<Uri> properties)
        {
            bool? result = true;

            if (HasPageOfEntity(entity))
            {
                result = false;
            }
            else
            {
                // otherwise check if we already have the pieces
                IEnumerable<JsonLdTriple> triples = null;

                lock (this)
                {
                    triples = _masterGraph.Triples.Where(t => StringComparer.Ordinal.Equals(entity.AbsoluteUri, t.Subject.GetValue()));
                }

                bool missing = false;

                foreach (var prop in properties)
                {
                    if (!triples.Where(t => StringComparer.Ordinal.Equals(prop.AbsoluteUri, t.Predicate.GetValue())).Any())
                    {
                        missing = true;
                        break;
                    }
                }

                if (!missing)
                {
                    result = null;
                }
            }

            return result;
        }

        public async Task Add(JObject compacted, Uri pageUri)
        {
            JsonLdPage page = new JsonLdPage(pageUri);

            if (!Utility.IsValidJsonLd(compacted))
            {
                DataTraceSources.Verbose("[EntityCache] Invalid JsonLd skipping {0}", pageUri.AbsoluteUri);
                return;
            }
            else
            {
                Uri rootUri = Utility.GetEntityUri(compacted);

                if (rootUri == null)
                {
                    // remove the blank node
                    string blankUrl = "http://blanknode.nuget.org/" + Guid.NewGuid().ToString();
                    compacted["@id"] = blankUrl;
                    DataTraceSources.Verbose("[EntityCache] BlankNode Doc {0}", blankUrl);
                }
            }

            lock (this)
            {
                if (_pages.Contains(page))
                {
                    // no work to do here.
                    return;
                }

                DataTraceSources.Verbose("[EntityCache] Added {0}", pageUri.AbsoluteUri);
                _pages.Enqueue(page);
            }

            HashSet<JToken> entityNodes = new HashSet<JToken>();

            Dictionary<int, JToken> nodes = new Dictionary<int,JToken>();
            int marker = 0;

            // Visitor
            Action<JObject> addSerial = (node) =>
            {
                if (!IsInContext(node))
                {
                    int serial = marker++;
                    node[CacheNode] = serial;
                    nodes.Add(serial, node);
                }
            };

            // add serials
            await JsonEntityVisitor(compacted, addSerial);

            var flattened = JsonLdProcessor.Flatten(compacted, new JsonLdOptions());
            BasicGraph graph = GetGraph(flattened);

            // split out the cache triples
            HashSet<Triple> cacheTriples = new HashSet<Triple>();
            HashSet<Triple> normalTriples = new HashSet<Triple>();

            foreach(var triple in graph.Triples)
            {
                if (StringComparer.Ordinal.Equals(triple.Predicate.GetValue(), CacheNode))
                {
                    cacheTriples.Add(triple);
                }
                else
                {
                    normalTriples.Add(triple);
                }
            }

            // create the real graph
            JsonLdGraphOld jsonGraph = new JsonLdGraphOld();

            var cacheGraph = new BasicGraph(cacheTriples);

            foreach (var triple in normalTriples)
            {
                var cacheTriple = cacheGraph.SelectSubject(triple.Subject.GetValue()).FirstOrDefault();

                JToken token = null;

                if (cacheTriple != null)
                {
                    int serial;
                    if (Int32.TryParse(cacheTriple.Object.GetValue(), out serial))
                    {
                        token = nodes[serial];
                    }
                }

                var jsonTriple = new JsonLdTriple(page, token, triple.Subject, triple.Predicate, triple.Object);
                jsonGraph.Assert(jsonTriple);
            }

            // put the json back to normal
            foreach(var node in nodes.Values)
            {
                JObject jObj = node as JObject;
                jObj.Remove(CacheNode);
            }

            lock (this)
            {
                _masterGraph.Merge(jsonGraph);
            }
        }

        public bool Reduce(int maxTriples)
        {
            bool reduced = false;

            lock (this)
            {
                while (_masterGraph.Count > maxTriples)
                {
                    var removePage = _pages.Dequeue();
                    _masterGraph.Triples.RemoveWhere(t => t.Page.Equals(removePage));
                    reduced = true;
                }
            }

            return reduced;
        }

        private const string CompactedIdName = "url";
        private const string ContextName = "@context";

        private static bool IsInContext(JToken token)
        {
            JToken parent = token;

            while (parent != null)
            {
                JProperty prop = parent as JProperty;

                if (prop != null && StringComparer.Ordinal.Equals(prop.Name, ContextName))
                {
                    return true;
                }

                parent = parent.Parent;
            }

            return false;
        }

        private static bool IsEntity(JToken jToken)
        {
            JProperty prop = jToken as JProperty;

            return prop != null && StringComparer.Ordinal.Equals(prop.Name, CompactedIdName);
        }

        public async Task JsonEntityVisitor(JObject root, Action<JObject> visitor)
        {
            var props = root
                .Descendants()
                .Where(t => t.Type == JTokenType.Property)
                .Cast<JProperty>()
                .Where(p => Utility.IdNames.Contains(p.Name))
                .ToList();

            foreach (var prop in props)
            {
                visitor((JObject)prop.Parent);
            }
        }

        private static HashSet<string> GetIdNames(JToken context)
        {
            var set = new HashSet<string>();
            JObject jObj = context as JObject;

            var props = jObj.Properties();
            foreach (var prop in props)
            {
                string val = prop.Value.ToString();

                // TODO: Make this more robust
                if (val == "@id" || val == "{\r\n  \"@type\": \"@id\"\r\n}")
                {
                    set.Add(prop.Name.ToString());
                }
            }

            return set;
        }

        public async Task<JToken> GetEntity(Uri entity)
        {
            JToken token = null;

            lock (this)
            {
                var triple = _masterGraph.Triples.Where(t => StringComparer.Ordinal.Equals(t.Subject.GetValue(), entity.AbsoluteUri)).FirstOrDefault();

                if (triple != null)
                {
                    token = triple.JsonNode;
                }
            }

            return token;
        }

        //public async Task<BasicGraph> DescribeRecursive(Uri entity)
        //{
        //    lock (this)
        //    {
        //        return _masterGraph.RecursiveDescribe(entity);
        //    }
        //}

        private static BasicGraph GetGraph(JToken flattened)
        {
            BasicGraph graph = new BasicGraph();

            RDFDataset dataSet = (RDFDataset)JsonLD.Core.JsonLdProcessor.ToRDF(flattened);

            foreach (var graphName in dataSet.GraphNames())
            {
                foreach (var quad in dataSet.GetQuads(graphName))
                {
                    graph.Assert(quad);
                }
            }

            return graph;
        }
    }
}
