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
    /// Represents the named graph.
    /// </summary>
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
            return Utility.CompareRootUris(_uri, uri);
        }

        public bool Equals(JsonLdPage other)
        {
            return Uri.Equals(other.Uri);
        }
    }
}
