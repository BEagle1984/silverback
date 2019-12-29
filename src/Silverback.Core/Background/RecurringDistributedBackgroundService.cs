// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Silverback.Background
{
    public abstract class RecurringDistributedBackgroundService : DistributedBackgroundService
    {
        private readonly TimeSpan _interval;
        private readonly ILogger<RecurringDistributedBackgroundService> _logger;

        protected RecurringDistributedBackgroundService(TimeSpan interval, IDistributedLockManager distributedLockManager, ILogger<RecurringDistributedBackgroundService> logger)
            : this(interval, null, distributedLockManager, logger)
        {
        }

        protected RecurringDistributedBackgroundService(TimeSpan interval, DistributedLockSettings distributedLockSettings, IDistributedLockManager distributedLockManager, ILogger<RecurringDistributedBackgroundService> logger)
            : base(distributedLockSettings, distributedLockManager, logger)
        {
            _interval = interval;
            _logger = logger;
        }

        protected override async Task ExecuteLockedAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Lock.Renew();

                await ExecuteRecurringAsync(stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                    break;

                await Sleep(stoppingToken);
            }

            _logger.LogInformation($"Background service {GetType().FullName} stopped.");
        }

        protected abstract Task ExecuteRecurringAsync(CancellationToken stoppingToken);

        private async Task Sleep(CancellationToken stoppingToken)
        {
            if (_interval <= TimeSpan.Zero)
                return;

            _logger.LogDebug($"Background service {GetType().FullName} " +
                             $"sleeping for {_interval.TotalMilliseconds} milliseconds.");

            await Task.Delay(_interval, stoppingToken);
        }
    }
}