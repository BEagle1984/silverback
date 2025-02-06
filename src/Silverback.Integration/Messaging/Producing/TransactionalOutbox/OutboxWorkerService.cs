// Copyright (c) 2024 Sergio Aquilini
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
    private readonly OutboxWorkerSettings _settings;

    private int _failedAttempts;

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
        _settings = Check.NotNull(settings, nameof(settings));
        OutboxWorker = Check.NotNull(outboxWorker, nameof(outboxWorker));
    }

    /// <summary>
    ///     Gets the associated <see cref="IOutboxWorker" />.
    /// </summary>
    public IOutboxWorker OutboxWorker { get; }

    /// <summary>
    ///     Calls the <see cref="IOutboxWorker" /> to process the queue.
    /// </summary>
    /// <inheritdoc cref="DistributedBackgroundService.ExecuteLockedAsync" />
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Handle any exception from any broker type")]
    protected override async Task ExecuteLockedAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (await OutboxWorker.ProcessOutboxAsync(stoppingToken).ConfigureAwait(false))
            {
                _failedAttempts = 0;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            _failedAttempts++;
            TimeSpan delay = IncrementalDelayHelper.Compute(
                _failedAttempts,
                _settings.InitialRetryDelay,
                _settings.RetryDelayIncrement,
                _settings.RetryDelayFactor,
                _settings.MaxRetryDelay) - _settings.Interval;

            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
        }
    }
}
