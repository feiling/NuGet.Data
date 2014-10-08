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
        private BasicGraph _masterGraph;

        public EntityCache()
        {
            _masterGraph = new BasicGraph();
        }

        public async Task Add(JToken compacted)
        {
            var flattened = JsonLdProcessor.Flatten(compacted, new JsonLdOptions());

            BasicGraph graph = GetGraph(flattened);

            await Task.Run(() =>
            {
                lock (this)
                {
                    _masterGraph.Merge(graph);
                }
            });
        }

        public async Task<BasicGraph> DescribeRecursive(Uri entity)
        {
            return await Task.Run(() => {
                lock (this)
                {
                    return _masterGraph.RecursiveDescribe(entity);
                }
            });
        }

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
