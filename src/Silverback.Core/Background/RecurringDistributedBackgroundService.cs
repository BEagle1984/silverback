// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;

namespace Silverback.Background
{
    /// <summary>
    ///     Extends the <see cref="DistributedBackgroundService" /> calling the execute method at regular
    ///     intervals. The distributed lock mechanism prevents concurrent executions.
    /// </summary>
    public abstract class RecurringDistributedBackgroundService : DistributedBackgroundService
    {
        private readonly TimeSpan _interval;

        private readonly ISilverbackLogger<RecurringDistributedBackgroundService> _logger;

        private bool _enabled = true;

        private CancellationTokenSource? _resumeTokenSource;

        /// <summary>
        ///     Initializes a new instance of the <see cref="RecurringDistributedBackgroundService" /> class using
        ///     the default settings for the lock mechanism.
        /// </summary>
        /// <param name="interval">
        ///     The interval between each execution.
        /// </param>
        /// <param name="distributedLockManager">
        ///     The <see cref="IDistributedLockManager" />.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ISilverbackLogger" />.
        /// </param>
        protected RecurringDistributedBackgroundService(
            TimeSpan interval,
            IDistributedLockManager distributedLockManager,
            ISilverbackLogger<RecurringDistributedBackgroundService> logger)
            : this(interval, null, distributedLockManager, logger)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="RecurringDistributedBackgroundService" /> class.
        /// </summary>
        /// <param name="interval">
        ///     The <see cref="TimeSpan" /> interval between each execution.
        /// </param>
        /// <param name="distributedLockSettings">
        ///     Customizes the lock mechanism settings.
        /// </param>
        /// <param name="distributedLockManager">
        ///     The <see cref="IDistributedLockManager" />.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ISilverbackLogger" />.
        /// </param>
        protected RecurringDistributedBackgroundService(
            TimeSpan interval,
            DistributedLockSettings? distributedLockSettings,
            IDistributedLockManager distributedLockManager,
            ISilverbackLogger<RecurringDistributedBackgroundService> logger)
            : base(distributedLockSettings, distributedLockManager, logger)
        {
            _interval = interval;
            _logger = logger;
        }

        /// <summary>
        ///     Pauses the execution of the recurring task.
        /// </summary>
        public void Pause()
        {
            _resumeTokenSource = new CancellationTokenSource();
            _enabled = false;
        }

        /// <summary>
        ///     Resumes the execution of the previously paused recurring task.
        /// </summary>
        public void Resume()
        {
            _resumeTokenSource!.Cancel();
            _enabled = true;
        }

        /// <inheritdoc cref="DistributedBackgroundService.ExecuteLockedAsync" />
        protected override async Task ExecuteLockedAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (Lock != null)
                    await Lock.Renew(stoppingToken).ConfigureAwait(false);

                if (_enabled)
                {
                    try
                    {
                        await ExecuteRecurringAsync(stoppingToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogInformation(
                            CoreEventIds.RecurringDistributedBackgroundServiceException,
                            ex,
                            "Background service {BackgroundService} recurring execution failed.",
                            GetType().FullName);
                        throw;
                    }
                }

                if (stoppingToken.IsCancellationRequested)
                    break;

                if (_enabled)
                    await Sleep(stoppingToken).ConfigureAwait(false);
                else
                    await Sleep(_resumeTokenSource!.Token).ConfigureAwait(false);
            }

            _logger.LogInformation(
                CoreEventIds.RecurringBackgroundServiceStopped,
                "Background service {BackgroundService} stopped.",
                GetType().FullName);
        }

        /// <summary>
        ///     This method is called at regular intervals after the <see cref="IHostedService" /> starts and the
        ///     lock is acquired. The implementation should return a task that represents the lifetime of the long
        ///     running operation(s) being performed.
        /// </summary>
        /// <param name="stoppingToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> that represents the long running operations.
        /// </returns>
        protected abstract Task ExecuteRecurringAsync(CancellationToken stoppingToken);

        private async Task Sleep(CancellationToken stoppingToken)
        {
            if (_interval <= TimeSpan.Zero)
                return;

            _logger.LogDebug(
                CoreEventIds.RecurringDistributedBackgroundServiceSleeping,
                "Background service {BackgroundService} sleeping for {sleepTimeInMilliseconds} milliseconds.",
                GetType().FullName,
                _interval.TotalMilliseconds);

            try
            {
                await Task.Delay(_interval, stoppingToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                // Ignored
            }
        }
    }
}
