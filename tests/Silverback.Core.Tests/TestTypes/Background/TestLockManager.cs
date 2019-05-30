// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Background;

namespace Silverback.Tests.Core.TestTypes.Background
{
    public class TestLockManager : IDistributedLockManager
    {
        private readonly List<string> _locks = new List<string>();

        public int Heartbeats { get; set; } = 0;

        public Task<DistributedLock> Acquire(DistributedLockSettings settings, CancellationToken cancellationToken = default) =>
            Acquire(settings.ResourceName, settings.AcquireTimeout, settings.AcquireRetryInterval);

        public async Task<DistributedLock> Acquire(string resourceName, TimeSpan? acquireTimeout = null, TimeSpan? acquireRetryInterval = null,
            TimeSpan? heartbeatTimeout = null, CancellationToken cancellationToken = default)
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
                            return new DistributedLock(resourceName, this, 50);
                        }
                    }
                }

                if (acquireRetryInterval != null)
                    await Task.Delay(acquireRetryInterval.Value.Milliseconds, cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                    break;
            }

            throw new TimeoutException("Couldn't get lock.");
        }

        public Task SendHeartbeat(string resourceName)
        {
            Heartbeats++;
            return Task.CompletedTask;
        }

        public Task Release(string resourceName)
        {
            lock (_locks)
            {
                _locks.Remove(resourceName);
            }

            return Task.CompletedTask;
        }
    }
}
