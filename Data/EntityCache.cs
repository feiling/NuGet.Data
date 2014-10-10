using JsonLD.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Data
{
    /// <summary>
    /// Thread safe cache of graphs.
    /// </summary>
    public class EntityCache
    {
        private readonly JsonLdGraph _masterGraph;
        private readonly Queue<JsonLdPage> _pages;
        private readonly ConcurrentQueue<Task> _addTasks;
        private const int _maxAdds = 5;

        // TODO: at what triple count does perf start to drop off?
        private const int _maxEntityCacheSize = 50000;

        public EntityCache()
        {
            _masterGraph = new JsonLdGraph();
            _pages = new Queue<JsonLdPage>();
            _addTasks = new ConcurrentQueue<Task>();
        }

        public bool HasPageOfEntity(Uri entity)
        {
            JsonLdPage page = new JsonLdPage(Utility.GetUriWithoutHash(entity));

            return _pages.Contains(page);
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

                WaitForTasks();

                lock (this)
                {
                    triples = _masterGraph.SelectSubject(entity);
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

        /// <summary>
        /// A minimally blocking call that starts a background task to update the graph.
        /// </summary>
        /// <param name="compacted"></param>
        /// <param name="pageUri"></param>
        public void Add(JObject compacted, Uri pageUri)
        {
            JsonLdPage page = new JsonLdPage(pageUri);

            // TODO: does this need a limiter?
            _addTasks.Enqueue(Task.Run(() => AddInternal(compacted, page)));
        }

        /// <summary>
        /// Waits until all queued tasks have completed. Do not run this from inside a lock!
        /// </summary>
        private void WaitForTasks()
        {
            while (_addTasks.Count > 0)
            {
                Task task = null;
                if (_addTasks.TryDequeue(out task))
                {
                    task.Wait();
                }
            }
        }

        private void AddInternal(JObject compacted, JsonLdPage page)
        {
            if (!Utility.IsValidJsonLd(compacted))
            {
                DataTraceSources.Verbose("[EntityCache] Invalid JsonLd skipping {0}", page.Uri.AbsoluteUri);
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
                    return;
                }

                _pages.Enqueue(page);
            }

            DataTraceSources.Verbose("[EntityCache] Added {0}", page.Uri.AbsoluteUri);

            // Load
            JsonLdGraph jsonGraph = JsonLdGraph.Load(compacted, page);

            lock (this)
            {
                // Reduce the cache before adding anything new, this could potentially block a while
                Reduce(_maxEntityCacheSize);

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
                    _masterGraph.RemovePage(removePage);
                    reduced = true;
                }
            }

            return reduced;
        }


        /// <summary>
        /// Look up the entity in the cache.
        /// </summary>
        public async Task<JToken> GetEntity(Uri entity)
        {
            return await Task<JToken>.Run(() =>
                {
                    JToken token = null;

                    DataTraceSources.Verbose("[EntityCache] GetEntity {0}", entity.AbsoluteUri);

                    WaitForTasks();

                    JsonLdTripleCollection triples = null;

                    lock (this)
                    {
                        // find the best JToken for this subject that we have
                        triples = _masterGraph.SelectSubject(entity);
                    }

                    JsonLdTriple triple = triples.Where(n => n.JsonNode != null).OrderByDescending(t => t.HasIdMatchingUrl ? 1 : 0).FirstOrDefault();

                    if (triple != null)
                    {
                        token = triple.JsonNode;
                    }

                    return token;
                });
        }
    }
}
