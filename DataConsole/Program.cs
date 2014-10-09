using Newtonsoft.Json.Linq;
using NuGet.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Run().Wait();
        }

        private static async Task Run()
        {
            try
            {
                JObject context = JObject.Parse(@"{
                                ""@context"": {
                                ""@vocab"": ""http://schema.nuget.org/schema#"",
                                ""nuget"": ""http://schema.nuget.org/schema#"",
                                ""catalog"": ""http://schema.nuget.org/catalog#"",
                                ""items"": {
                                    ""@id"": ""catalog:item"",
                                    ""@container"": ""@set""
                                },
                                ""url"": ""@id"",
                                ""commitTimeStamp"": {
                                    ""@id"": ""catalog:commitTimeStamp"",
                                    ""@type"": ""http://www.w3.org/2001/XMLSchema#dateTime""
                                },
                                ""commitId"": {
                                    ""@id"": ""catalog:commitId""
                                },
                                ""count"": {
                                    ""@id"": ""catalog:count""
                                },
                                ""packageTargetFrameworks"": {
                                    ""@container"": ""@set"",
                                    ""@id"": ""packageTargetFramework""
                                },
                                ""dependencyGroups"": {
                                    ""@container"": ""@set"",
                                    ""@id"": ""dependencyGroup""
                                },
                                ""dependencies"": {
                                    ""@container"": ""@set"",
                                    ""@id"": ""dependency""
                                },
                                ""nupkgUrl"": {
                                    ""@type"": ""@id""
                                },
                                ""registration"": {
                                    ""@type"": ""@id""
                                }
                                }}");

                DataClient cache = new DataClient(context);

                //Uri packageInfoUri = new Uri("https://nugetjohtaylo.blob.core.windows.net/ver31/registration/ajaxcontroltoolkit/index.json");
                Uri packageInfoUri = new Uri("http://nugetjohtaylo.blob.core.windows.net/ver3/registration/newtonsoft.json/index.json");

                CacheHttpClient client = new CacheHttpClient();
                var jObj3 = await client.GetJObjectAsync(packageInfoUri);

                JToken jObj = await cache.GetFile(packageInfoUri);

                JToken jObj2 = await cache.GetFile(packageInfoUri);

                var entity = await cache.GetEntity(new Uri("http://nugetjohtaylo.blob.core.windows.net/ver31/registration/ajaxcontroltoolkit/index.json#page/1.0.0/7.1213.0"));

                var blah = await cache.Ensure(entity, new Uri[] { new Uri("http://schema.nuget.org/schema#commitId") });

                var id = blah["commitId"];

                ////var myObj = await cache.GetInclusiveView(new Uri("http://nugetjohtaylo.blob.core.windows.net/ver3/registration/entityframework/index.json"));

                //using (StreamWriter writer = new StreamWriter(@"d:\out.json"))
                //{
                //    writer.Write(myObj.ToString());
                //}
            }
            catch (Exception ex)
            {
                Debug.Fail(ex.ToString());
            }
        }
    }
}
