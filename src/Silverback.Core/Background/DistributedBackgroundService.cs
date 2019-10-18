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

        protected DistributedLock Lock;

        protected DistributedBackgroundService(IDistributedLockManager distributedLockManager, ILogger<DistributedBackgroundService> logger)
            : this(null, distributedLockManager, logger)
        {
        }

        protected DistributedBackgroundService(DistributedLockSettings distributedLockSettings, IDistributedLockManager distributedLockManager, ILogger<DistributedBackgroundService> logger)
        {
            _distributedLockSettings = distributedLockSettings ?? new DistributedLockSettings();
            _distributedLockManager = distributedLockManager ?? throw new ArgumentNullException(nameof(distributedLockManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (string.IsNullOrEmpty(_distributedLockSettings.ResourceName))
                _distributedLockSettings.ResourceName = GetType().FullName;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"Starting background service {GetType().FullName}...");

            // Run another task to avoid deadlocks
            // ReSharper disable once MethodSupportsCancellation
            return Task.Run(async () =>
            {
                try
                {
                    Lock = await _distributedLockManager.Acquire(_distributedLockSettings, stoppingToken);

                    _logger.LogInformation($"Lock acquired, executing background service {GetType().FullName}.");

                    await ExecuteLockedAsync(stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // Don't log exception that is fired by the cancellation token.
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Background service '{GetType().FullName}' failed.");
                }
                finally
                {
                    if (Lock != null)
                        await Lock.Release();
                }
            });
        }

        protected abstract Task ExecuteLockedAsync(CancellationToken stoppingToken);
    }
}
