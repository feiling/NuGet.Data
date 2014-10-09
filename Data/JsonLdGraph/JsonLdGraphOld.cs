using JsonLD.Core;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Node = JsonLD.Core.RDFDataset.Node;

namespace NuGet.Data
{
    public class JsonLdGraphOld
    {
        private readonly HashSet<JsonLdTriple> _triples;

        public JsonLdGraphOld()
            : this(new HashSet<JsonLdTriple>())
        {

        }
        
        public JsonLdGraphOld(HashSet<JsonLdTriple> triples)
        {
            _triples = triples;
        }

        public void Assert(JsonLdTriple triple)
        {
            Triples.Add(triple);
        }

        public int Count
        {
            get
            {
                return Triples.Count;
            }
        }

        public void Merge(JsonLdGraphOld graph)
        {
            foreach(var triple in graph.Triples)
            {
                if (Triples.Contains(triple) && triple.HasIdMatchingUrl)
                {
                    // swap with the real copy
                    Triples.Remove(triple);
                    Triples.Add(triple);
                }
                else
                {
                    Triples.Add(triple);
                }
            }
        }

        public HashSet<JsonLdTriple> Triples
        {
            get
            {
                return _triples;
            }
        }
    }

}
