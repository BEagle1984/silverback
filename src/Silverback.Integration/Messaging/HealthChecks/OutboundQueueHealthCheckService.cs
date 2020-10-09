// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Outbound.TransactionalOutbox.Repositories;

namespace Silverback.Messaging.HealthChecks
{
    /// <inheritdoc cref="IOutboundQueueHealthCheckService" />
    public class OutboundQueueHealthCheckService : IOutboundQueueHealthCheckService
    {
        private static readonly TimeSpan DefaultMaxAge = TimeSpan.FromSeconds(30);

        private readonly IOutboxReader _queueReader;

        /// <summary>
        ///     Initializes a new instance of the <see cref="OutboundQueueHealthCheckService" /> class.
        /// </summary>
        /// <param name="queueReader">
        ///     The <see cref="IOutboxReader" />.
        /// </param>
        public OutboundQueueHealthCheckService(IOutboxReader queueReader)
        {
            _queueReader = queueReader;
        }

        /// <inheritdoc cref="IOutboundQueueHealthCheckService.CheckIsHealthy" />
        public async Task<bool> CheckIsHealthy(TimeSpan? maxAge = null, int? maxQueueLength = null)
        {
            if (maxQueueLength != null &&
                await _queueReader.GetLengthAsync().ConfigureAwait(false) > maxQueueLength)
                return false;

            return await _queueReader.GetMaxAgeAsync().ConfigureAwait(false) <= (maxAge ?? DefaultMaxAge);
        }
    }
}
