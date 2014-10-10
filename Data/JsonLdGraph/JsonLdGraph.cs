using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JsonLD.Core;
using Node = JsonLD.Core.RDFDataset.Node;

namespace NuGet.Data
{
    public class JsonLdGraph : JsonLdTripleCollection
    {
        // triples containing the most complete JTokens
        private readonly HashSet<JsonLdTriple> _triples;

        // additional triples which contain links to less useful JTokens
        private readonly List<JsonLdTriple> _alternativeTriples;

        private readonly Dictionary<Node, JsonLdTriple> _subjectIndex;

        public JsonLdGraph(IEnumerable<JsonLdTriple> triples)
            : base(null)
        {
            _triples = new HashSet<JsonLdTriple>(triples);
            _alternativeTriples = new List<JsonLdTriple>();
            _subjectIndex = new Dictionary<Node, JsonLdTriple>();
        }

        public void Assert(JsonLdTriple triple)
        {
            lock (this)
            {
                // TODO: fix this
                if (_triples.Contains(triple) && triple.HasIdMatchingUrl)
                {
                    // swap with the real copy
                    _triples.Remove(triple);
                    _triples.Add(triple);
                }
                else
                {
                    _triples.Add(triple);
                }
            }
        }

        public override int Count
        {
            get
            {
                lock (this)
                {
                    return _triples.Count;
                }
            }
        } 

        public void Assert(JsonLdPage page, JToken jsonNode, Node subNode, Node predNode, Node objNode)
        {
            _triples.Add(new JsonLdTriple(page, jsonNode, subNode, predNode, objNode));
        }

        public void Merge(JsonLdGraph graph)
        {
            foreach (var triple in graph.Triples)
            {
                
            }
        }

        /// <summary>
        /// triples containing the most complete JTokens
        /// </summary>
        public override IEnumerable<JsonLdTriple> Triples
        {
            get
            {
                lock (this)
                {
                    return _triples.ToArray();
                }
            }
        }

        /// <summary>
        /// additional triples which contain links to less useful JTokens
        /// </summary>
        public IEnumerable<JsonLdTriple> AlternativeTriples
        {
            get
            {
                lock (this)
                {
                    return _alternativeTriples.ToArray();
                }
            }
        }
    }
}
