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
    public class JsonLdGraph
    {
        private readonly HashSet<JsonLdTriple> _triples;

        public JsonLdGraph()
            : this(new HashSet<JsonLdTriple>())
        {

        }
        
        public JsonLdGraph(HashSet<JsonLdTriple> triples)
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

        public void Merge(JsonLdGraph graph)
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

    public class JsonLdPage : IEquatable<JsonLdPage>
    {
        private readonly Uri _uri;

        public JsonLdPage(Uri uri)
        {
            _uri = uri;
        }

        public Uri Uri
        {
            get
            {
                return _uri;
            }
        }

        public bool IsEntityFromPage(Uri uri)
        {
            string s = uri.AbsoluteUri;

            if (s.IndexOf('#') > -1)
            {
                s = s.Split('#')[0];
            }

            return StringComparer.Ordinal.Equals(_uri.AbsoluteUri, s);
        }

        public bool Equals(JsonLdPage other)
        {
            return Uri.Equals(other.Uri);
        }
    }

    public class JsonLdTriple : Triple
    {
        private readonly JToken _jsonNode;
        private readonly JsonLdPage _jsonPage;

        public JsonLdTriple(JsonLdPage page, JToken jsonNode, RDFDataset.Node subNode, RDFDataset.Node predNode, RDFDataset.Node objNode)
            : base(subNode, predNode, objNode)
        {
            _jsonNode = jsonNode;
            _jsonPage = page;
        }

        public JToken JsonNode
        {
            get
            {
                return _jsonNode;
            }
        }

        public bool HasIdMatchingUrl
        {
            get
            {
                return Page.IsEntityFromPage(new Uri(Subject.GetValue()));
            }
        }

        public JsonLdPage Page
        {
            get
            {
                return _jsonPage;
            }
        }
    }
}
