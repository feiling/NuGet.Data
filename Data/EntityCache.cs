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

            lock (this)
            {
                return _pages.Contains(page);
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

        public void Add(JObject compacted, Uri pageUri)
        {
            // this is probably thread safe enough, but just to avoid too many dequeues and waits at the same time
            lock (this)
            {
                Reduce(_maxEntityCacheSize);

                if (_addTasks.Count >= _maxAdds)
                {
                    // force a wait if we are over the limit
                    Task curTask = null;
                    if (_addTasks.TryDequeue(out curTask))
                    {
                        curTask.Wait();
                    }
                }

                _addTasks.Enqueue(Task.Run(() => AddInternal(compacted, pageUri)));
            }
        }

        private void AddInternal(JObject compacted, Uri pageUri)
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

            JsonLdGraph jsonGraph = JsonLdGraph.Load(compacted, page);

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
                    _masterGraph.RemovePage(removePage);
                    reduced = true;
                }
            }

            return reduced;
        }


        public async Task<JToken> GetEntity(Uri entity)
        {
            return await Task<JToken>.Run(() =>
                {
                    JToken token = null;

                    lock (this)
                    {
                        // find the best JToken for this subject that we have
                        JsonLdTriple triple = _masterGraph.SelectSubject(entity).Where(n => n.JsonNode != null).OrderByDescending(t => t.HasIdMatchingUrl ? 1 : 0).FirstOrDefault();

                        if (triple != null)
                        {
                            token = triple.JsonNode;
                        }
                    }

                    return token;
                });
        }
    }
}
