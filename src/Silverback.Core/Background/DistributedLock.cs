// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Background
{
    public class DistributedLock
    {
        private readonly DistributedLockSettings _settings;
        private readonly IDistributedLockManager _lockManager;

        public DistributedLock(DistributedLockSettings settings, IDistributedLockManager lockManager)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _lockManager = lockManager ?? throw new ArgumentNullException(nameof(lockManager));

            Status = DistributedLockStatus.Acquired;

            Task.Run(SendHeartbeats);
        }

        public DistributedLockStatus Status { get; private set; }

        /// <summary>
        ///     Ensures that the lock is still valid, otherwise tries to re-acquire it.
        /// </summary>
        /// <returns></returns>
        public async Task Renew(CancellationToken cancellationToken = default)
        {
            if (Status == DistributedLockStatus.Released)
                throw new InvalidOperationException("This lock was explicitly released and cannot be renewed.");

            await CheckIsStillLocked();

            if (Status == DistributedLockStatus.Acquired)
            {
                await _lockManager.SendHeartbeat(_settings);
            }
            else
            {
                await _lockManager.Acquire(_settings, cancellationToken);
                Status = DistributedLockStatus.Acquired;
            }
        }

        public async Task Release()
        {
            Status = DistributedLockStatus.Released;
            await _lockManager.Release(_settings);
        }

        private async Task CheckIsStillLocked()
        {
            if (Status != DistributedLockStatus.Acquired)
                return;

            if (!await _lockManager.CheckIsStillLocked(_settings))
                Status = DistributedLockStatus.Lost;
        }

        private async Task SendHeartbeats()
        {
            var failedHeartbeats = 0;

            while (Status != DistributedLockStatus.Released)
            {
                if (Status == DistributedLockStatus.Acquired)
                {
                    failedHeartbeats =
                        !await _lockManager.SendHeartbeat(_settings)
                            ? failedHeartbeats + 1
                            : 0;

                    if (failedHeartbeats >= _settings.FailedHeartbeatsThreshold)
                        await CheckIsStillLocked();
                }

                await Task.Delay(_settings.HeartbeatInterval);
            }
        }
    }
}