// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Silverback.Messaging.HealthChecks
{
    /// <summary>
    ///     Checks that all outbound endpoints are reachable.
    /// </summary>
    public interface IProducersHealthCheckService
    {
        /// <summary>
        ///     Produces a <see cref="PingMessage" /> to all configured producers.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains an
        ///     <see cref="EndpointCheckResult" /> for each endpoint.
        /// </returns>
        Task<IReadOnlyCollection<EndpointCheckResult>> SendPingMessagesAsync();
    }
}
