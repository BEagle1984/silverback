// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Silverback.Messaging.Connectors.Repositories;

namespace Silverback.Messaging.HealthChecks
{
    /// <inheritdoc cref="IOutboundQueueHealthCheckService" />
    public class OutboundQueueHealthCheckService : IOutboundQueueHealthCheckService
    {
        private static readonly TimeSpan DefaultMaxAge = TimeSpan.FromSeconds(30);

        private readonly IOutboundQueueReader _queueReader;

        /// <summary>
        ///     Initializes a new instance of the <see cref="OutboundQueueHealthCheckService" /> class.
        /// </summary>
        /// <param name="queueReader">
        ///     The <see cref="IOutboundQueueReader" />.
        /// </param>
        public OutboundQueueHealthCheckService(IOutboundQueueReader queueReader)
        {
            _queueReader = queueReader;
        }

        /// <inheritdoc cref="IOutboundQueueHealthCheckService.CheckIsHealthy" />
        public async Task<bool> CheckIsHealthy(TimeSpan? maxAge = null, int? maxQueueLength = null)
        {
            if (maxQueueLength != null &&
                await _queueReader.GetLength().ConfigureAwait(false) > maxQueueLength)
                return false;

            return await _queueReader.GetMaxAge().ConfigureAwait(false) <= (maxAge ?? DefaultMaxAge);
        }
    }
}
