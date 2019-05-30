// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Silverback.Background
{
    /// <summary>
    /// Extends the <seealso cref="Microsoft.Extensions.Hosting.BackgroundService" /> adding a distributed lock mechanism to prevent
    /// concurrent executions.
    /// </summary>
    /// <seealso cref="Microsoft.Extensions.Hosting.BackgroundService" />
    public abstract class DistributedBackgroundService : BackgroundService
    {
        private readonly DistributedLockSettings _distributedLockSettings;
        private readonly IDistributedLockManager _distributedLockManager;
        private readonly ILogger<DistributedBackgroundService> _logger;
        private DistributedLock _acquiredLock;

        protected DistributedBackgroundService(DistributedLockSettings distributedLockSettings, IDistributedLockManager distributedLockManager, ILogger<DistributedBackgroundService> logger)
        {
            _distributedLockSettings = distributedLockSettings ?? throw new ArgumentNullException(nameof(distributedLockSettings));
            _distributedLockManager = distributedLockManager ?? throw new ArgumentNullException(nameof(distributedLockManager));
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                $"Starting background service {GetType().FullName}. Waiting for lock '{_distributedLockSettings.ResourceName}'...");

            // Run another task to avoid deadlocks
            return Task.Run(async () =>
            {
                try
                {
                    _acquiredLock = await _distributedLockManager.Acquire(_distributedLockSettings);

                    _logger.LogInformation($"Lock acquired, executing background service {GetType().FullName}.");

                    await ExecuteLockedAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        $"Background service '{GetType().FullName}' failed. See inner exception for details.");
                }
                finally
                {
                    if (_acquiredLock != null)
                        await _acquiredLock.Release();
                }
            });
        }

        protected abstract Task ExecuteLockedAsync(CancellationToken stoppingToken);
    }
}
