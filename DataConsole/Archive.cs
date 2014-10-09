//using Newtonsoft.Json.Linq;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace CacheConsole
//{
//    class Archive
//    {
//        /// <summary>
//        /// Returns all known things about the id, fetching it if needed, but making only 1 fetch max.
//        /// </summary>
//        /// <param name="id"></param>
//        /// <returns></returns>
//        public async Task<JToken> GetInclusiveView(Uri id)
//        {
//            return await DescribeRecursive(id, _context);
//        }

//        /// <summary>
//        /// Limited to only the critical properties.
//        /// </summary>
//        /// <param name="id"></param>
//        /// <param name="ensureProperties"></param>
//        /// <param name="context"></param>
//        /// <returns></returns>
//        public async Task<JToken> GetExclusiveView(Uri id, IEnumerable<Uri> criticalProperties, JToken context, bool allowFetch)
//        {
//            throw new NotImplementedException();
//        }

//        private async Task<JToken> DescribeRecursive(Uri entity, JToken context)
//        {
//            HashSet<Triple> triples = new HashSet<Triple>();

//            var graph = await _entityCache.DescribeRecursive(entity);

//            //graph.SelectSubject(entity).SelectPredicate(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#type"));

//            return await CreateJson(graph, context, entity);
//        }

//        private async Task<JToken> CreateJson(BasicGraph graph, JToken context, Uri frameSchemaType)
//        {
//            var jObj = JsonLdProcessor.FromRDF(new JValue(graph.NQuads));

//            //foreach (var node in graph.SelectPredicate(new Uri("http://nugetjohtaylo.blob.core.windows.net/ver3/registration/entityframework/index.json")).Triples)
//            //{
//            //    var x = node.Object.GetValue();
//            //}

//            JObject frame = new JObject();

//            var innerContext = context["@context"];

//            if (context != null)
//            {
//                frame["@context"] = context["@context"];
//            }

//            frame["@type"] = "http://schema.nuget.org/schema#Package";

//            var flattened = JsonLdProcessor.Flatten(jObj, innerContext, new JsonLdOptions());
//            var framed = JsonLdProcessor.Frame(flattened, frame, new JsonLdOptions());
//            var compacted = JsonLdProcessor.Compact(framed, innerContext, new JsonLdOptions());

//            return compacted;
//        }
//    }
//}
