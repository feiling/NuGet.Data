using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Data
{
    public class NullFileCache : FileCacheBase
    {

        public NullFileCache()
            : base()
        {

        }

        public override void Add(FileCacheEntry entry)
        {
            // do nothing
        }

        public override bool TryGet(Uri uri, out FileCacheEntry entry)
        {
            // do nothing
            entry = null;
            return false;
        }

        public override void Remove(Uri uri)
        {
            // do nothing
        }
    }
}
