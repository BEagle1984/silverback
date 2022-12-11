// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Silverback.Diagnostics;
using Silverback.Lock;
using Silverback.Util;

namespace Silverback.Background;

/// <summary>
///     Extends the <see cref="Microsoft.Extensions.Hosting.BackgroundService" /> adding a distributed lock
///     mechanism to prevent concurrent executions.
/// </summary>
public abstract class DistributedBackgroundService : BackgroundService
{
    private readonly ISilverbackLogger<DistributedBackgroundService> _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DistributedBackgroundService" /> class.
    /// </summary>
    /// <param name="distributedLock">
    ///     The <see cref="IDistributedLock" />.
    /// </param>
    /// <param name="logger">
    ///     The <see cref="ISilverbackLogger" />.
    /// </param>
    protected DistributedBackgroundService(
        IDistributedLock distributedLock,
        ISilverbackLogger<DistributedBackgroundService> logger)
    {
        DistributedLock = Check.NotNull(distributedLock, nameof(distributedLock));
        _logger = Check.NotNull(logger, nameof(logger));
    }

    /// <summary>
    ///     Gets the <see cref="IDistributedLock" /> used by this service.
    /// </summary>
    protected IDistributedLock DistributedLock { get; }

    /// <inheritdoc cref="BackgroundService.ExecuteAsync" />
    [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
    protected sealed override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogBackgroundServiceStarting(this);

        // Run another task to avoid deadlocks
        return Task.Run(
            async () =>
            {
                try
                {
                    await AcquireLockAndExecuteAsync(stoppingToken).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    // Don't log exception that is fired by the cancellation token.
                }
                catch (Exception ex)
                {
                    _logger.LogBackgroundServiceException(this, ex);
                }
            },
            stoppingToken);
    }

    /// <summary>
    ///     Acquires the lock and calls the <see cref="ExecuteLockedAsync" /> method to perform the operation(s).
    /// </summary>
    /// <param name="stoppingToken">
    ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    protected virtual async Task AcquireLockAndExecuteAsync(CancellationToken stoppingToken)
    {
        DistributedLockHandle lockHandle = await DistributedLock.AcquireAsync(stoppingToken).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable disposable = lockHandle.ConfigureAwait(false);
        _logger.LogBackgroundServiceLockAcquired(this);
        await ExecuteLockedAsync(stoppingToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     This method is called after the lock is acquired to perform the actual operation(s). The implementation should return a task
    ///     that represents the lifetime of the operation(s) being performed.
    /// </summary>
    /// <param name="stoppingToken">
    ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    protected abstract Task ExecuteLockedAsync(CancellationToken stoppingToken);
}
