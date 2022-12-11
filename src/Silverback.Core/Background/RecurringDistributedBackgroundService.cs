// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Lock;

namespace Silverback.Background;

/// <summary>
///     Extends the <see cref="DistributedBackgroundService" /> calling the execute method at regular
///     intervals. The distributed lock mechanism prevents concurrent executions.
/// </summary>
public abstract class RecurringDistributedBackgroundService : DistributedBackgroundService
{
    private readonly TimeSpan _interval;

    private readonly ISilverbackLogger<RecurringDistributedBackgroundService> _logger;

    private bool _enabled = true;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RecurringDistributedBackgroundService" /> class.
    /// </summary>
    /// <param name="interval">
    ///     The <see cref="TimeSpan" /> interval between each execution.
    /// </param>
    /// <param name="distributedLock">
    ///     The <see cref="IDistributedLock" />.
    /// </param>
    /// <param name="logger">
    ///     The <see cref="ISilverbackLogger" />.
    /// </param>
    protected RecurringDistributedBackgroundService(
        TimeSpan interval,
        IDistributedLock distributedLock,
        ISilverbackLogger<RecurringDistributedBackgroundService> logger)
        : base(distributedLock, logger)
    {
        _interval = interval;
        _logger = logger;
    }

    /// <summary>
    ///     Pauses the execution of the recurring task.
    /// </summary>
    public void Pause() => _enabled = false;

    /// <summary>
    ///     Resumes the execution of the previously paused recurring task.
    /// </summary>
    public void Resume() => _enabled = true;

    /// <inheritdoc cref="DistributedBackgroundService.AcquireLockAndExecuteAsync" />
    protected override async Task AcquireLockAndExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            DistributedLockHandle lockHandle = await DistributedLock.AcquireAsync(stoppingToken).ConfigureAwait(false);

            await using (lockHandle)
            {
                _logger.LogBackgroundServiceLockAcquired(this);

                while (!lockHandle.IsLost && !stoppingToken.IsCancellationRequested)
                {
                    if (_enabled)
                    {
                        await ExecuteLockedAsync(stoppingToken).ConfigureAwait(false);

                        if (stoppingToken.IsCancellationRequested)
                            break;

                        await SleepAsync(_interval, stoppingToken).ConfigureAwait(false);
                    }
                    else
                    {
                        await SleepAsync(TimeSpan.FromMilliseconds(100), stoppingToken).ConfigureAwait(false);
                    }
                }
            }
        }

        _logger.LogRecurringBackgroundServiceStopped(this);
    }

    private async Task SleepAsync(TimeSpan delay, CancellationToken stoppingToken)
    {
        if (delay <= TimeSpan.Zero)
            return;

        _logger.LogRecurringBackgroundServiceSleeping(this, delay);

        try
        {
            await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Ignored
        }
    }
}
