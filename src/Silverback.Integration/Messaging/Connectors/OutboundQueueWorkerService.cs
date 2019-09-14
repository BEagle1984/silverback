// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Background;

namespace Silverback.Messaging.Connectors
{
    public class OutboundQueueWorkerService : RecurringDistributedBackgroundService
    {
        private readonly IOutboundQueueWorker _outboundQueueWorker;

        public OutboundQueueWorkerService(TimeSpan interval, IOutboundQueueWorker outboundQueueWorker, DistributedLockSettings distributedLockSettings,
            IDistributedLockManager distributedLockManager, ILogger<OutboundQueueWorkerService> logger)
            : base(interval, distributedLockSettings, distributedLockManager, logger)
        {
            _outboundQueueWorker = outboundQueueWorker;
        }

        protected override Task ExecuteRecurringAsync(CancellationToken stoppingToken) =>
            _outboundQueueWorker.ProcessQueue(stoppingToken);
    }
}