// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Util;

namespace Silverback.Background
{
    /// <summary>
    ///     Represents a lock that has been acquired through an <see cref="IDistributedLockManager" />.
    /// </summary>
    public class DistributedLock
    {
        private readonly IDistributedLockManager _lockManager;

        private readonly DistributedLockSettings _settings;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DistributedLock" /> class.
        /// </summary>
        /// <param name="settings">
        ///     Specifies all settings of the lock to be acquired.
        /// </param>
        /// <param name="lockManager">
        ///     The <see cref="IDistributedLockManager" /> that generated the lock and can be used to keep it alive
        ///     and finally release it.
        /// </param>
        public DistributedLock(DistributedLockSettings settings, IDistributedLockManager lockManager)
        {
            _settings = Check.NotNull(settings, nameof(settings));
            _lockManager = Check.NotNull(lockManager, nameof(lockManager));

            Status = DistributedLockStatus.Acquired;

            Task.Factory.StartNew(
                SendHeartbeatsAsync,
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        /// <summary>
        ///     Gets the lock status.
        /// </summary>
        public DistributedLockStatus Status { get; private set; }

        /// <summary>
        ///     Ensures that the lock is still valid, otherwise tries to re-acquire it.
        /// </summary>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        public async Task RenewAsync(CancellationToken cancellationToken = default)
        {
            if (Status == DistributedLockStatus.Released)
                throw new InvalidOperationException("This lock was explicitly released and cannot be renewed.");

            await CheckIsStillLockedAsync().ConfigureAwait(false);

            if (Status == DistributedLockStatus.Acquired)
            {
                await _lockManager.SendHeartbeatAsync(_settings).ConfigureAwait(false);
            }
            else
            {
                await _lockManager.AcquireAsync(_settings, cancellationToken).ConfigureAwait(false);
                Status = DistributedLockStatus.Acquired;
            }
        }

        /// <summary>
        ///     Releases the lock.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        public async Task ReleaseAsync()
        {
            Status = DistributedLockStatus.Released;
            await _lockManager.ReleaseAsync(_settings).ConfigureAwait(false);
        }

        private async Task CheckIsStillLockedAsync()
        {
            if (Status != DistributedLockStatus.Acquired)
                return;

            if (!await _lockManager.CheckIsStillLockedAsync(_settings).ConfigureAwait(false))
                Status = DistributedLockStatus.Lost;
        }

        private async Task SendHeartbeatsAsync()
        {
            var failedHeartbeats = 0;

            while (Status != DistributedLockStatus.Released)
            {
                if (Status == DistributedLockStatus.Acquired)
                {
                    failedHeartbeats =
                        !await _lockManager.SendHeartbeatAsync(_settings).ConfigureAwait(false)
                            ? failedHeartbeats + 1
                            : 0;

                    if (failedHeartbeats >= _settings.FailedHeartbeatsThreshold)
                        await CheckIsStillLockedAsync().ConfigureAwait(false);
                }

                await Task.Delay(_settings.HeartbeatInterval).ConfigureAwait(false);
            }
        }
    }
}
