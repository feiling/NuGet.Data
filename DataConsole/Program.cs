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

                DataClient cache = new DataClient(new CacheHttpClient(), new MemoryFileCache(), context);

                //Uri packageInfoUri = new Uri("http://nugetjohtaylo.blob.core.windows.net/ver3/registration/microsoft.bcl/index.json");
                Uri packageInfoUri = new Uri("http://nugetjohtaylo.blob.core.windows.net/ver3/registration/newtonsoft.json/index.json");

                var task = cache.GetFile(packageInfoUri);
                var task2 = cache.GetFile(packageInfoUri);
                var task3 = cache.GetFile(packageInfoUri);
                var task4 = cache.GetFile(packageInfoUri);
                var task5 = cache.GetFile(packageInfoUri);

                var jObj = await task2;
                var jObj2 = await task;

                EntityCache ec = new EntityCache();
                ec.Add(jObj, packageInfoUri);

                var entity = await cache.GetEntity(new Uri("http://nugetjohtaylo.blob.core.windows.net/ver3/registration/newtonsoft.json/index.json"));

                var blah = await cache.Ensure(entity, new Uri[] { new Uri("http://schema.nuget.org/schema#commitId") });

                var id = blah["commitId"];

                var search = await cache.GetFile(new Uri("http://preview-search.nuget.org/search/query"));

                var newtonsoft = search["data"].First;

                var ensureBlank = await cache.Ensure(newtonsoft, new Uri[] { });

                var ensureProp = await cache.Ensure(newtonsoft, new Uri[] { new Uri("http://schema.nuget.org/schema#commitId") });

                var id2 = ensureProp["commitId"];

                
                var rootUri = new Uri("http://nugetjohtaylo.blob.core.windows.net/ver33/registrations/newtonsoft.json/index.json");

                var rootEntity = await cache.GetEntity(rootUri);

                var rootEnsure = await cache.Ensure(rootEntity, new Uri[] { new Uri("http://schema.nuget.org/schema#commitId") });
                var rootCommitId = rootEnsure["commitId"];

                var rootEnsure2 = await cache.Ensure(rootEntity, new Uri[] { new Uri("http://schema.nuget.org/schema#nonexistant") });
            }
            catch (Exception ex)
            {
                Debug.Fail(ex.ToString());
            }
        }
    }
}
