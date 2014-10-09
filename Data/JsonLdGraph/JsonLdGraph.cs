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


        public JsonLdGraph(IReadOnlyCollection<JsonLdTriple> triples)
            : base(null)
        {

        }


    }

}
