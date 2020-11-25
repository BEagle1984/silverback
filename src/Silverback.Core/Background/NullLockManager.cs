// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Background
{
    /// <summary>
    ///     This implementation of <see cref="IDistributedLockManager" /> doesn't actually acquire nor check any
    ///     lock. Is is used when the <see cref="NullLockSettings" /> are specified or no other
    ///     <see cref="IDistributedLockManager" /> is registered.
    /// </summary>
    public class NullLockManager : IDistributedLockManager
    {
        /// <inheritdoc cref="IDistributedLockManager.AcquireAsync" />
        public Task<DistributedLock?> AcquireAsync(
            DistributedLockSettings settings,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<DistributedLock?>(null);

        /// <inheritdoc cref="IDistributedLockManager.CheckIsStillLockedAsync" />
        public Task<bool> CheckIsStillLockedAsync(DistributedLockSettings settings) =>
            Task.FromResult(true);

        /// <inheritdoc cref="IDistributedLockManager.SendHeartbeatAsync" />
        public Task<bool> SendHeartbeatAsync(DistributedLockSettings settings) =>
            Task.FromResult(true);

        /// <inheritdoc cref="IDistributedLockManager.ReleaseAsync" />
        public Task ReleaseAsync(DistributedLockSettings settings) =>
            Task.CompletedTask;
    }
}
