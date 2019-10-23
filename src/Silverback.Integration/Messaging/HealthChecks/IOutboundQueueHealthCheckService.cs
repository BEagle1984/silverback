// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;

namespace Silverback.Messaging.HealthChecks
{
    public interface IOutboundQueueHealthCheckService
    {
        /// <summary>
        /// Checks the age of the messages stored in the outbound queue (outbox table) and
        /// optionally the queue size.
        /// </summary>
        /// <param name="maxAge">The maximum message age, the check will fail when a message exceeds this age (default is 30 seconds).</param>
        /// <param name="maxQueueLength">The maximum amount of messages in the queue (default is null, meaning unrestricted).</param>
        /// <returns></returns>
        Task<bool> CheckIsHealthy(TimeSpan? maxAge = null, int? maxQueueLength = null);
    }
}