// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Silverback.Messaging.HealthChecks
{
    public interface IOutboundEndpointsHealthCheckService
    {
        /// <summary>
        /// Produces a <see cref="PingMessage"/> to all configured outbound endpoints.
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<EndpointCheckResult>> PingAllEndpoints();
    }
}