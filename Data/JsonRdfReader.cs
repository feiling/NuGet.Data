//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;
//using System.IO;

//namespace NuGet.Data
//{
//    public class JsonRdfReader
//    {
//        public JsonRdfReader()
//        {
            
//        }

//        public BasicGraph Load(TextReader input)
//        {
           

//            BasicGraph graph = new BasicGraph();

//                // Perform actual parsing
//            using (JsonReader jsonReader = new JsonTextReader(input))
//            {
//                jsonReader.DateParseHandling = DateParseHandling.None;

//                JToken json = JToken.Load(jsonReader);

//                foreach (JObject subjectJObject in json)
//                {
//                    string subject = subjectJObject["@id"].ToString();

//                    JToken type;
//                    if (subjectJObject.TryGetValue("@type", out type))
//                    {
//                        if (type is JArray)
//                        {
//                            foreach (JToken t in (JArray)type)
//                            {
//                                if (!HandleTriple(graph, subject, "http://www.w3.org/1999/02/22-rdf-syntax-ns#type", t.ToString(), null, false)) return graph;
//                            }
//                        }
//                        else
//                        {
//                            if (!HandleTriple(graph, subject, "http://www.w3.org/1999/02/22-rdf-syntax-ns#type", type.ToString(), null, false)) return graph;
//                        }
//                    }

//                    foreach (JProperty property in subjectJObject.Properties())
//                    {
//                        if (property.Name == "@id" || property.Name == "@type")
//                        {
//                            continue;
//                        }

//                        foreach (JObject objectJObject in property.Value)
//                        {
//                            JToken id;
//                            JToken value;
//                            if (objectJObject.TryGetValue("@id", out id))
//                            {
//                                if (!HandleTriple(graph, subject, property.Name, id.ToString(), null, false)) return graph;
//                            }
//                            else if (objectJObject.TryGetValue("@value", out value))
//                            {
//                                string datatype = null;
//                                JToken datatypeJToken;
//                                if (objectJObject.TryGetValue("@type", out datatypeJToken))
//                                {
//                                    datatype = datatypeJToken.ToString();
//                                }
//                                else
//                                {
//                                    switch (value.Type)
//                                    {
//                                        case JTokenType.Boolean:
//                                            datatype = "http://www.w3.org/2001/XMLSchema#boolean";
//                                            break;
//                                        case JTokenType.Float:
//                                            datatype = "http://www.w3.org/2001/XMLSchema#double";
//                                            break;
//                                        case JTokenType.Integer:
//                                            datatype = "http://www.w3.org/2001/XMLSchema#integer";
//                                            break;
//                                    }
//                                }

//                                if (!HandleTriple(graph, subject, property.Name, value.ToString(), datatype, true)) return graph;
//                            }
//                        }
//                    }
//                }
//            }

//            return graph;
//        }

//          /// <summary>
//        /// Creates and handles a triple
//        /// </summary>
//        /// <param name="handler">Handler</param>
//        /// <param name="subject">Subject</param>
//        /// <param name="predicate">Predicate</param>
//        /// <param name="obj">Object</param>
//        /// <param name="datatype">Object Datatype</param>
//        /// <param name="isLiteral">isLiteral Object</param>
//        /// <returns>True if parsing should continue, false otherwise</returns>
//        bool HandleTriple(BasicGraph graph, string subject, string predicate, string obj, string datatype, bool isLiteral)
//        {
//            Node subjectNode;
//            if (subject.StartsWith("_"))
//            {
//                string nodeId = subject.Substring(subject.IndexOf(":") + 1);
//                subjectNode = new Node(null);
//            }
//            else
//            {
//                subjectNode = new UriNode(new Uri(subject));
//            }

//            Node predicateNode = new UriNode(new Uri(predicate));

//            Node objNode;
//            if (isLiteral)
//            {
//                if (datatype == "http://www.w3.org/2001/XMLSchema#boolean")
//                {
//                    //  sometimes newtonsoft.json appears to return boolean as string True and dotNetRdf doesn't appear to recognize that
//                    obj = ((string)obj).ToLowerInvariant();
//                }

//                //objNode = (datatype == null) ? new Node((string)obj) : new Node(((string)obj, new Uri(datatype));
//                // TODO: Add datatype support again!
//                objNode = new Node((string)obj);
//            }
//            else
//            {
//                if (obj.StartsWith("_"))
//                {
//                    string nodeId = obj.Substring(obj.IndexOf(":") + 1);
//                    objNode = new Node(null);
//                }
//                else
//                {
//                    objNode = new UriNode(new Uri(obj));
//                }
//            }

//            graph.Assert(new Triple(subjectNode, predicateNode, objNode));

//            return true;
//        }
//    }

//}
