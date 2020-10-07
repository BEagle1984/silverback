// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Silverback.Background;
using Silverback.Diagnostics;

namespace Silverback.Messaging.Outbound.TransactionalOutbox
{
    /// <summary>
    ///     The <see cref="IHostedService" /> that triggers the outbound queue worker at regular intervals.
    /// </summary>
    public class OutboxWorkerService : RecurringDistributedBackgroundService
    {
        private readonly IOutboxWorker _outboxWorker;

        /// <summary>
        ///     Initializes a new instance of the <see cref="OutboxWorkerService" /> class.
        /// </summary>
        /// <param name="interval">
        ///     The interval between each execution.
        /// </param>
        /// <param name="outboxWorker">
        ///     The <see cref="IOutboxWorker" /> implementation.
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
        public OutboxWorkerService(
            TimeSpan interval,
            IOutboxWorker outboxWorker,
            DistributedLockSettings distributedLockSettings,
            IDistributedLockManager distributedLockManager,
            ISilverbackLogger<OutboxWorkerService> logger)
            : base(interval, distributedLockSettings, distributedLockManager, logger)
        {
            _outboxWorker = outboxWorker;
        }

        /// <summary>
        ///     Calls the <see cref="IOutboxWorker" /> to process the queue at regular intervals.
        /// </summary>
        /// <inheritdoc cref="RecurringDistributedBackgroundService.ExecuteRecurringAsync" />
        protected override Task ExecuteRecurringAsync(CancellationToken stoppingToken) =>
            _outboxWorker.ProcessQueueAsync(stoppingToken);
    }
}
