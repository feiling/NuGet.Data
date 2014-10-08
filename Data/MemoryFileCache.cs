using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Data
{
    public class MemoryFileCache : FileCacheBase
    {
        private readonly ConcurrentDictionary<Uri, FileCacheEntry> _entries;

        public MemoryFileCache()
            : base()
        {
            _entries = new ConcurrentDictionary<Uri, FileCacheEntry>();
        }

        public override void Add(FileCacheEntry entry)
        {
            _entries.AddOrUpdate(entry.Uri, entry, (k, v) => entry);
        }

        public override bool TryGet(Uri uri, out FileCacheEntry entry)
        {
            return _entries.TryGetValue(uri, out entry);
        }

        public override void Remove(Uri uri)
        {
            FileCacheEntry entry = null;
            _entries.TryRemove(uri, out entry);
        }

    }
}
