// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Silverback.Messaging.Connectors.Repositories;

namespace Silverback.Messaging.HealthChecks
{
    public class OutboundQueueHealthCheckService : IOutboundQueueHealthCheckService
    {
        private static readonly TimeSpan DefaultMaxAge = TimeSpan.FromSeconds(30);

        private readonly IOutboundQueueConsumer _queue;

        public OutboundQueueHealthCheckService(IOutboundQueueConsumer queue)
        {
            _queue = queue;
        }

        public async Task<bool> CheckIsHealthy(TimeSpan? maxAge = null, int? maxQueueLength = null)
        {
            if (maxQueueLength != null &&
                await _queue.GetLength() > maxQueueLength)
                return false;

            return await _queue.GetMaxAge() <= (maxAge ?? DefaultMaxAge);
        }
    }
}