using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Data
{
    public class BrowserFileCache : FileCacheBase
    {

        public override void Add(FileCacheEntry entry)
        {
            throw new NotImplementedException();
        }

        public override void Remove(Uri uri)
        {
            throw new NotImplementedException();
        }

        public override bool TryGet(Uri uri, out FileCacheEntry entry)
        {
            throw new NotImplementedException();
        }

    }
}
