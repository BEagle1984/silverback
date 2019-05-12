using System;
using System.Collections.Generic;
using System.Threading;
using Silverback.Background;

namespace Silverback.Tests.Core.TestTypes.Background
{
    public class TestLockManager : IDistributedLockManager
    {
        private readonly List<string> _locks = new List<string>();

        public IDisposable Acquire(string resourceName, DistributedLockSettings settings = null) =>
            Acquire(resourceName, settings?.AcquireTimeout, settings?.AcquireRetryInterval);

        public IDisposable Acquire(string resourceName, TimeSpan? acquireTimeout = null, TimeSpan? acquireRetryInterval = null,
            TimeSpan? heartbeatTimeout = null)
        {
            var start = DateTime.Now;
            while (acquireTimeout == null || DateTime.Now - start < acquireTimeout)
            {
                if (!_locks.Contains(resourceName))
                {
                    lock (_locks)
                    {
                        if (!_locks.Contains(resourceName))
                        {
                            _locks.Add(resourceName);
                            return new DistributedLock(resourceName, this);
                        }
                    }
                }

                if (acquireRetryInterval != null)
                    Thread.Sleep(acquireRetryInterval.Value.Milliseconds);
            }

            throw new TimeoutException("Couldn't get lock.");
        }

        public void SendHeartbeat(string resourceName)
        {
            // Not yet implemented
        }

        public void Release(string resourceName)
        {
            lock (_locks)
            {
                _locks.Remove(resourceName);
            }
        }
    }
}
