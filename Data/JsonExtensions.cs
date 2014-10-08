using JsonLD.Core;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Data
{
    public static class JsonExtensions
    {

        public static void EnsureProperty(this JToken jToken, DataClient cache, string property, JToken context)
        {
            JsonLdProcessor.Frame(jToken, context, new JsonLdOptions());
        }

        public static void EnsureProperty(this JToken jToken, DataClient cache, string property)
        {
            var context = jToken.NearestContext();

            string s = context.ToString();

            JObject frame = JObject.Parse("{" + s + "}");
            frame.Add("@type", "http://schema.nuget.org/schema#galleryDetailsUrl");

            var framed = JsonLdProcessor.Frame(jToken, context, new JsonLdOptions());
        }

        /// <summary>
        /// Returns the context nearest above or on this jObject
        /// </summary>
        /// <param name="jObject"></param>
        /// <returns></returns>
        public static JToken NearestContext(this JToken jToken)
        {
            JToken parent = jToken;

            JToken context = null;
            while (parent != null && context == null)
            {
                context = parent.Where(n => n.Path == "@context").FirstOrDefault();

                parent = parent.Parent;
            }

            return context;
        }
    }
}
