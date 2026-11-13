// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Silverback.Background;
using Silverback.Diagnostics;
using Silverback.Lock;
using Silverback.Util;

namespace Silverback.Messaging.Producing.TransactionalOutbox;

/// <summary>
///     The <see cref="IHostedService" /> that triggers the <see cref="IOutboxWorker" /> at regular intervals.
/// </summary>
public class OutboxWorkerService : RecurringDistributedBackgroundService
{
    // Bounds the counter in any case (e.g. with a delay factor lower than 1, never reaching the max delay)
    private const int MaxCountedFailedAttempts = 1000;

    // Minimum delay between the skipped rounds, not to spin while suspended or yielding to a manual pass with a short interval
    private static readonly TimeSpan SkippedRoundDelay = TimeSpan.FromMilliseconds(100);

    // Serializes the scheduled passes and the manual ones triggered by the testing helpers
    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "No wait handle is used, and disposing it would race with the shutdown")]
    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

    private int _failedAttempts;

    private int _pendingManualPasses;

    /// <summary>
    ///     Initializes a new instance of the <see cref="OutboxWorkerService" /> class.
    /// </summary>
    /// <param name="settings">
    ///     The <see cref="OutboxWorkerSettings" />.
    /// </param>
    /// <param name="outboxWorker">
    ///     The <see cref="IOutboxWorker" /> implementation.
    /// </param>
    /// <param name="distributedLock">
    ///     The <see cref="IDistributedLock" />.
    /// </param>
    /// <param name="logger">
    ///     The <see cref="ISilverbackLogger" />.
    /// </param>
    public OutboxWorkerService(
        OutboxWorkerSettings settings,
        IOutboxWorker outboxWorker,
        IDistributedLock distributedLock,
        ISilverbackLogger<OutboxWorkerService> logger)
        : base(Check.NotNull(settings, nameof(settings)).Interval, distributedLock, logger)
    {
        Settings = Check.NotNull(settings, nameof(settings));
        OutboxWorker = Check.NotNull(outboxWorker, nameof(outboxWorker));
    }

    /// <summary>
    ///     Gets the associated <see cref="IOutboxWorker" />.
    /// </summary>
    public IOutboxWorker OutboxWorker { get; }

    /// <summary>
    ///     Gets the <see cref="OutboxWorkerSettings" />.
    /// </summary>
    internal OutboxWorkerSettings Settings { get; }

    /// <summary>
    ///     Gets a value indicating whether the scheduled processing is currently suspended.
    /// </summary>
    internal bool IsAutomaticProcessingSuspended { get; private set; }

    // The built-in OutboxWorker reports the passes that failed to produce some messages without throwing, the other implementations are
    // trusted to return true only when the batch was successfully processed
    private bool HasLastPassFailed => OutboxWorker is OutboxWorker { LastPassFailed: true };

    private bool HasLastPassFailedWithoutProgress => OutboxWorker is OutboxWorker { LastPassFailed: true, LastPassProducedCount: 0 };

    /// <summary>
    ///     Processes the outbox right away, until it is empty. The exceptions are propagated to the caller.
    /// </summary>
    /// <remarks>
    ///     The distributed lock is intentionally bypassed, since this is meant to be used in tests only. The pass is serialized with the
    ///     scheduled ones.
    /// </remarks>
    /// <param name="cancellationToken">
    ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    internal async Task ProcessUntilEmptyAsync(CancellationToken cancellationToken)
    {
        // Signals the scheduled loop to yield after the current batch, to not wait for it to drain (or to loop forever on failing messages)
        Interlocked.Increment(ref _pendingManualPasses);

        try
        {
            await _processingSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            Interlocked.Decrement(ref _pendingManualPasses);
        }

        try
        {
            // Throw on the failed passes, otherwise the messages that keep failing would be retried forever
            while (await OutboxWorker.ProcessOutboxAsync(cancellationToken).ConfigureAwait(false))
            {
                if (HasLastPassFailed)
                    throw new OutboxProcessingException("Some messages couldn't be produced (see the logs).");
            }
        }
        finally
        {
            _processingSemaphore.Release();
        }
    }

    /// <summary>
    ///     Suspends the scheduled processing right away. The returned <see cref="Task" /> completes when any running pass is finished.
    /// </summary>
    /// <param name="cancellationToken">
    ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    internal Task SuspendAutomaticProcessingAsync(CancellationToken cancellationToken)
    {
        SuspendAutomaticProcessing();
        return WaitForRunningPassAsync(cancellationToken);
    }

    /// <summary>
    ///     Suspends the scheduled processing right away, without waiting for the running pass (if any) to complete.
    /// </summary>
    internal void SuspendAutomaticProcessing() => IsAutomaticProcessingSuspended = true;

    /// <summary>
    ///     Waits for the running pass (if any) to complete.
    /// </summary>
    /// <remarks>
    ///     The semaphore operations also order a previous <see cref="SuspendAutomaticProcessing" /> with the scheduled passes. The
    ///     suspension is not reverted if the wait is canceled.
    /// </remarks>
    /// <param name="cancellationToken">
    ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    internal async Task WaitForRunningPassAsync(CancellationToken cancellationToken)
    {
        await _processingSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        _processingSemaphore.Release();
    }

    /// <summary>
    ///     Resumes the scheduled processing.
    /// </summary>
    internal void ResumeAutomaticProcessing() => IsAutomaticProcessingSuspended = false;

    /// <summary>
    ///     Calls the <see cref="IOutboxWorker" /> to process the queue.
    /// </summary>
    /// <inheritdoc cref="DistributedBackgroundService.ExecuteLockedAsync" />
    protected override async Task ExecuteLockedAsync(CancellationToken stoppingToken)
    {
        // The round is skipped (and not considered a failure) while a manual pass is running
        TimeSpan delay = await _processingSemaphore.WaitAsync(0, stoppingToken).ConfigureAwait(false)
            ? await ProcessScheduledRoundAsync(stoppingToken).ConfigureAwait(false)
            : SkippedRoundDelay;

        // The delay is awaited after releasing the semaphore, to not block the manual passes. The interval is awaited by the base class.
        delay -= Settings.Interval;

        if (delay > TimeSpan.Zero)
            await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
    }

    // Must be called holding the semaphore, which is released at the end. Returns the delay before the next round.
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Handle any exception from any broker type")]
    private async Task<TimeSpan> ProcessScheduledRoundAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Checked after acquiring the semaphore, to be ordered with the write done by SuspendAutomaticProcessing
            if (IsAutomaticProcessingSuspended)
                return SkippedRoundDelay;

            bool failed = false;

            try
            {
                // The suspension and the manual passes waiting for the semaphore are checked between the batches, to yield after the
                // current one instead of draining
                while (!IsAutomaticProcessingSuspended &&
                       Volatile.Read(ref _pendingManualPasses) == 0 &&
                       await OutboxWorker.ProcessOutboxAsync(stoppingToken).ConfigureAwait(false))
                {
                    // A failed pass doesn't throw if the message order isn't enforced or the produce fails asynchronously. It is delayed
                    // like any other failure when nothing could be produced, not to retry the same failing messages in a tight loop,
                    // but the processing goes on while some messages are produced, not to let the failing ones throttle the others
                    if (HasLastPassFailedWithoutProgress)
                    {
                        failed = true;
                        break;
                    }

                    _failedAttempts = 0;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                failed = true;
            }

            if (!failed)
                return TimeSpan.Zero;

            TimeSpan retryDelay = IncrementalDelayHelper.Compute(
                _failedAttempts + 1,
                Settings.InitialRetryDelay,
                Settings.RetryDelayIncrement,
                Settings.RetryDelayFactor,
                Settings.MaxRetryDelay);

            // Stop counting once the max delay is reached, not to overflow the computation while the messages keep failing
            if (_failedAttempts < MaxCountedFailedAttempts && (Settings.MaxRetryDelay == null || retryDelay < Settings.MaxRetryDelay.Value))
                _failedAttempts++;

            return retryDelay;
        }
        finally
        {
            _processingSemaphore.Release();
        }
    }
}
