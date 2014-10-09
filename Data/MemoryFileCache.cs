using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Data
{
    public class MemoryFileCache : FileCacheBase
    {
        private readonly ConcurrentDictionary<Uri, Stream> _entries;

        public MemoryFileCache()
            : base()
        {
            _entries = new ConcurrentDictionary<Uri, Stream>();
        }

        public override void Remove(Uri uri)
        {
            Stream entry = null;
            _entries.TryRemove(uri, out entry);
        }

        public override bool TryGet(Uri uri, out Stream stream)
        {
            return _entries.TryGetValue(uri, out stream);
        }

        public override void Add(Uri uri, TimeSpan lifeSpan, Stream stream)
        {
            _entries.AddOrUpdate(uri, stream, (k, v) => stream);
        }
    }
}
