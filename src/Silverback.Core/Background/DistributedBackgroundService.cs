// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Util;

namespace Silverback.Background
{
    /// <summary>
    ///     Extends the <see cref="Microsoft.Extensions.Hosting.BackgroundService" /> adding a distributed lock
    ///     mechanism to prevent concurrent executions.
    /// </summary>
    public abstract class DistributedBackgroundService : BackgroundService
    {
        private readonly IDistributedLockManager _distributedLockManager;

        private readonly DistributedLockSettings _distributedLockSettings;

        private readonly ILogger<DistributedBackgroundService> _logger;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DistributedBackgroundService" /> class using the
        ///     default settings for the lock mechanism.
        /// </summary>
        /// <param name="distributedLockManager">
        ///     The <see cref="IDistributedLockManager" />.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ILogger" />.
        /// </param>
        protected DistributedBackgroundService(
            IDistributedLockManager distributedLockManager,
            ILogger<DistributedBackgroundService> logger)
            : this(null, distributedLockManager, logger)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DistributedBackgroundService" /> class.
        /// </summary>
        /// <param name="distributedLockSettings">
        ///     Customizes the lock mechanism settings.
        /// </param>
        /// <param name="distributedLockManager">
        ///     The <see cref="IDistributedLockManager" />.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ILogger" />.
        /// </param>
        protected DistributedBackgroundService(
            DistributedLockSettings? distributedLockSettings,
            IDistributedLockManager distributedLockManager,
            ILogger<DistributedBackgroundService> logger)
        {
            _distributedLockSettings = distributedLockSettings ?? new DistributedLockSettings();

            _distributedLockSettings.EnsureResourceNameIsSet(GetType().FullName);

            _distributedLockManager = Check.NotNull(distributedLockManager, nameof(distributedLockManager));
            _logger = Check.NotNull(logger, nameof(logger));
        }

        /// <summary>
        ///     Gets the acquired <see cref="DistributedLock" />.
        /// </summary>
        protected DistributedLock? Lock { get; private set; }

        /// <inheritdoc cref="BackgroundService.ExecuteAsync" />
        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                EventIds.DistributedBackgroundServiceStartingBackgroundService,
                "Starting background service {BackgroundService}...",
                GetType().FullName);

            // Run another task to avoid deadlocks
            return Task.Run(
                async () =>
                {
                    try
                    {
                        Lock = await _distributedLockManager.Acquire(_distributedLockSettings, stoppingToken)
                            .ConfigureAwait(false);

                        if (Lock != null)
                        {
                            _logger.LogInformation(
                                EventIds.DistributedBackgroundServiceExecute,
                                "Lock acquired, executing background service {BackgroundService}.",
                                GetType().FullName);
                        }

                        await ExecuteLockedAsync(stoppingToken).ConfigureAwait(false);
                    }
                    catch (TaskCanceledException)
                    {
                        // Don't log exception that is fired by the cancellation token.
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            EventIds.DistributedBackgroundServiceUnhandledException,
                            ex,
                            "Background service '{BackgroundService}' failed.",
                            GetType().FullName);
                    }
                    finally
                    {
                        if (Lock != null)
                            await Lock.Release().ConfigureAwait(false);
                    }
                },
                stoppingToken);
        }

        /// <summary>
        ///     This method is called when the <see cref="IHostedService" /> starts and the lock is acquired. The
        ///     implementation should return a task that represents the lifetime of the long running operation(s)
        ///     being performed.
        /// </summary>
        /// <param name="stoppingToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> that represents the long running operations.
        /// </returns>
        protected abstract Task ExecuteLockedAsync(CancellationToken stoppingToken);
    }
}
