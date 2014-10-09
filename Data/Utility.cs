using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Data
{
    public static class Utility
    {
        public static bool IsValidJsonLd(JObject compacted)
        {
            return compacted != null && GetEntityUri(compacted) != null;
        }

        public static readonly string[] IdNames = new string[] { "url", "@id" };

        public static Uri GetUriWithoutHash(Uri uri)
        {
            if (uri != null)
            {
                string s = uri.AbsoluteUri;
                int hash = s.IndexOf('#');

                if (hash > -1)
                {
                    s = s.Substring(0, hash);
                    return new Uri(s);
                }
            }

            return uri;
        }

        /// <summary>
        /// Check if the entity url matches the root url
        /// </summary>
        /// <param name="token">entity token</param>
        /// <param name="entityUri">Optional field, if this is given the method will not try to parse it out again.</param>
        /// <returns>true if the root uri is the base of the entity uri</returns>
        public static bool? IsEntityFromPage(JToken token, Uri entityUri = null)
        {
            bool? result = null;
            Uri uri = entityUri == null ? GetEntityUri(token) : entityUri;

            if (uri != null)
            {
                var rootUri = GetEntityUri(GetRoot(token));

                result = CompareRootUris(uri, rootUri);
            }

            return result;
        }

        /// <summary>
        /// Checks if the uris match or differ only in the # part. 
        /// If either are null this returns false.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool CompareRootUris(Uri a, Uri b)
        {
            if (a == null || b == null)
            {
                return false;
            }

            var x = Utility.GetUriWithoutHash(a);
            var y = Utility.GetUriWithoutHash(b);

            return x.Equals(y);
        }

        public static JToken GetRoot(JToken token)
        {
            JToken parent = token;

            while (parent.Parent != null)
            {
                parent = parent.Parent;
            }

            return parent;
        }

        public static Uri GetEntityUri(JObject jObj)
        {
            if (jObj != null)
            {
                JToken urlValue;

                foreach (string idName in IdNames)
                {
                    if (jObj.TryGetValue(idName, out urlValue))
                    {
                        return new Uri(urlValue.ToString());
                    }
                }
            }

            return null;
        }

        public static Uri GetEntityUri(JToken token)
        {
            return GetEntityUri(token as JObject);
        }

        public static async Task<JToken> FindEntityInJson(Uri entity, JObject json)
        {
            string search = entity.AbsoluteUri;

            var idNode = json.Descendants().Concat(json).Where(n =>
            {
                JProperty prop = n as JProperty;

                if (prop != null)
                {
                    string url = prop.Value.ToString();

                    return StringComparer.Ordinal.Equals(url, search);
                }

                return false;
            }).FirstOrDefault();

            if (idNode != null)
            {
                return idNode.Parent.DeepClone();
            }

            return null;
        }
    }
}
