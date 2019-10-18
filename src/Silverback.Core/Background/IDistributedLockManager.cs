// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Background
{
    public interface IDistributedLockManager
    {
        Task<DistributedLock> Acquire(string resourceName, string uniqueId, TimeSpan? acquireTimeout = null, TimeSpan? acquireRetryInterval = null, TimeSpan? heartbeatTimeout = null, CancellationToken cancellationToken = default);

        Task<DistributedLock> Acquire(DistributedLockSettings settings, CancellationToken cancellationToken = default);
        
        Task<bool> CheckIsStillLocked(DistributedLockSettings settings);

        Task<bool> SendHeartbeat(DistributedLockSettings settings);

        Task Release(DistributedLockSettings settings);
    }
}