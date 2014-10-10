using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DataTest
{
    public class JsonLdGraphTests
    {

        [Fact]
        public void JsonLdGraph_Basic()
        {
            JObject compacted = BasicGraph;

            JsonLdPage page = new JsonLdPage(new Uri("http://test/doc"));

            JsonLdGraph graph = JsonLdGraph.Load(compacted, page);

            Assert.Equal(10, graph.Triples.Count());
            Assert.Equal(0, graph.AlternativeTriples.Count());
        }

        private JObject BasicGraph
        {
            get
            {
                return JObject.Parse(@"{
                  ""@context"": {
                    ""@vocab"": ""http://schema.test#"",
                    ""test"": ""http://schema.test#"",
                    ""items"": {
                      ""@id"": ""test#Child"",
                      ""@container"": ""@set""
                    },
                    ""hasProperty"": {
                      ""@container"": ""@set""
                    },
                    ""xsd"": ""http://www.w3.org/2001/XMLSchema#""
                  },
                  ""@id"": ""http://test/doc"",
                  ""@type"": ""Main"",
                  ""test:items"": [
                    {
                      ""@id"": ""http://test/doc#a"",
                      ""@type"": ""Child"",
                      ""test:info"": {
                        ""@id"": ""http://test/doc#c"",
                        ""@type"": ""PartialEntity"",
                        ""name"": ""grandChildC"",
                        ""partialEntityDescription"": {
                          ""@id"": ""http://test/doc#grandChildDescription"",
                          ""@type"": ""test:PartialEntityDescription"",
                          ""hasProperty"": [
                            ""http://schema.test#name"",
                            ""http://schema.test#title""
                          ]
                        }
                      },
                      ""test:name"": ""childA""
                    },
                    {
                      ""@id"": ""http://test/doc#b"",
                      ""@type"": ""Child"",
                      ""info"": {
                        ""@id"": ""http://test/doc#d"",
                        ""@type"": ""GrandChild""
                      },
                      ""name"": ""childB""
                    }
                  ],
                  ""name"": ""test""
                }");
            }
        }
    }
}
