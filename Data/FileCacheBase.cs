using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Data
{
    public abstract class FileCacheBase
    {
        public abstract bool TryGet(Uri uri, out FileCacheEntry entry);

        public abstract void Remove(Uri uri);

        public abstract void Add(FileCacheEntry entry);
    }
}
