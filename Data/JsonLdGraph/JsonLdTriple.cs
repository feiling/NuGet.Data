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
    /// <summary>
    /// A triple + page and JToken of the subject
    /// </summary>
    public class JsonLdTriple : Triple
    {
        private readonly JToken _jsonNode;
        private readonly JsonLdPage _jsonPage;

        public JsonLdTriple(JsonLdPage page, JToken jsonNode, Node subNode, Node predNode, Node objNode)
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
