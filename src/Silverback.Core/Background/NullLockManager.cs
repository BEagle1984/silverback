// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Background
{
    /// <summary>
    ///     This implementation of <see cref="IDistributedLockManager" /> doesn't actually acquire nor
    ///     check any
    ///     lock. Is is used when the <see cref="NullLockSettings" /> are specified or no other
    ///     <see cref="IDistributedLockManager" /> is registered.
    /// </summary>
    public class NullLockManager : IDistributedLockManager
    {
        /// <inheritdoc />
        public Task<DistributedLock?> Acquire(
            DistributedLockSettings settings,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<DistributedLock?>(null);

        /// <inheritdoc />
        public Task<bool> CheckIsStillLocked(DistributedLockSettings settings) =>
            Task.FromResult(true);

        /// <inheritdoc />
        public Task<bool> SendHeartbeat(DistributedLockSettings settings) =>
            Task.FromResult(true);

        /// <inheritdoc />
        public Task Release(DistributedLockSettings settings) =>
            Task.CompletedTask;
    }
}
