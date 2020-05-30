// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Silverback.Background;

namespace Silverback.Messaging.Connectors
{
    /// <summary>
    ///     The <see cref="IHostedService" /> that triggers the outbound queue worker at regular intervals.
    /// </summary>
    public class OutboundQueueWorkerService : RecurringDistributedBackgroundService
    {
        private readonly IOutboundQueueWorker _outboundQueueWorker;

        /// <summary>
        ///     Initializes a new instance of the <see cref="OutboundQueueWorkerService" /> class.
        /// </summary>
        /// <param name="interval">
        ///     The interval between each execution.
        /// </param>
        /// <param name="outboundQueueWorker">
        ///     The <see cref="IOutboundQueueWorker" /> implementation.
        /// </param>
        /// <param name="distributedLockSettings">
        ///     Customizes the lock mechanism settings.
        /// </param>
        /// <param name="distributedLockManager">
        ///     The <see cref="IDistributedLockManager" />.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ILogger" />.
        /// </param>
        public OutboundQueueWorkerService(
            TimeSpan interval,
            IOutboundQueueWorker outboundQueueWorker,
            DistributedLockSettings distributedLockSettings,
            IDistributedLockManager distributedLockManager,
            ILogger<OutboundQueueWorkerService> logger)
            : base(interval, distributedLockSettings, distributedLockManager, logger)
        {
            _outboundQueueWorker = outboundQueueWorker;
        }

        /// <summary>
        ///     Calls the <see cref="IOutboundQueueWorker" /> to process the queue at regular intervals.
        /// </summary>
        /// <inheritdoc cref="RecurringDistributedBackgroundService.ExecuteRecurringAsync" />
        protected override Task ExecuteRecurringAsync(CancellationToken stoppingToken) =>
            _outboundQueueWorker.ProcessQueue(stoppingToken);
    }
}
