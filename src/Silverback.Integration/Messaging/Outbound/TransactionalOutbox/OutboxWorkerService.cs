// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Silverback.Background;
using Silverback.Diagnostics;
using Silverback.Lock;

namespace Silverback.Messaging.Outbound.TransactionalOutbox;

/// <summary>
///     The <see cref="IHostedService" /> that triggers the <see cref="IOutboxWorker" /> at regular intervals.
/// </summary>
public class OutboxWorkerService : RecurringDistributedBackgroundService
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="OutboxWorkerService" /> class.
    /// </summary>
    /// <param name="interval">
    ///     The interval between each execution.
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
        TimeSpan interval,
        IOutboxWorker outboxWorker,
        IDistributedLock distributedLock,
        ISilverbackLogger<OutboxWorkerService> logger)
        : base(interval, distributedLock, logger)
    {
        OutboxWorker = outboxWorker;
    }

    /// <summary>
    ///     Gets the associated <see cref="IOutboxWorker" />.
    /// </summary>
    public IOutboxWorker OutboxWorker { get; }

    /// <summary>
    ///     Calls the <see cref="IOutboxWorker" /> to process the queue.
    /// </summary>
    /// <inheritdoc cref="RecurringDistributedBackgroundService.ExecuteLockedAsync" />
    protected override async Task ExecuteLockedAsync(CancellationToken stoppingToken)
    {
        while (await OutboxWorker.ProcessOutboxAsync(stoppingToken).ConfigureAwait(false))
        {
        }
    }
}
