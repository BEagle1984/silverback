// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;

namespace Silverback.Messaging.HealthChecks
{
    /// <summary>
    ///     Checks that the outbound queue is being processed at a sustainable pace.
    /// </summary>
    public interface IOutboundQueueHealthCheckService
    {
        /// <summary>
        ///     Checks the age of the messages stored in the transactional outbox and optionally the queue length.
        /// </summary>
        /// <param name="maxAge">
        ///     The maximum message age, the check will fail when a message exceeds this age (default is 30
        ///     seconds).
        /// </param>
        /// <param name="maxQueueLength">
        ///     The maximum amount of messages in the queue (default is null, meaning unrestricted).
        /// </param>
        /// <returns>
        ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains a
        ///     boolean value indicating whether the check is successful.
        /// </returns>
        Task<bool> CheckIsHealthy(TimeSpan? maxAge = null, int? maxQueueLength = null);
    }
}
