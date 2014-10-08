using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NuGet.Data
{
    /// <summary>
    /// Session wide URI lock
    /// </summary>
    public class UriLock : IDisposable
    {
        // session wide locks
        private static readonly ConcurrentDictionary<Uri, object> _locks = new ConcurrentDictionary<Uri, object>();
        private readonly Uri _uri;
        private readonly int _msWait;

        public UriLock(Uri uri, int msWait=100)
        {
            _uri = uri;
            _msWait = msWait;

            GetLock();
        }

        private void GetLock()
        {
            object obj = new object();

            while (!_locks.TryAdd(_uri, obj))
            {
                // spin lock
                Thread.Sleep(_msWait);
            }
        }

        private void ReleaseLock()
        {
            object obj = null;
            if (!_locks.TryRemove(_uri, out obj))
            {
                Debug.Fail("Missing lock object!");
            }
        }

        public void Dispose()
        {
            ReleaseLock();
        }
    }
}
