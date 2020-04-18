// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Background
{
    public interface IDistributedLockManager
    {
        Task<DistributedLock> Acquire(DistributedLockSettings settings, CancellationToken cancellationToken = default);

        Task<bool> CheckIsStillLocked(DistributedLockSettings settings);

        Task<bool> SendHeartbeat(DistributedLockSettings settings);

        Task Release(DistributedLockSettings settings);
    }
}