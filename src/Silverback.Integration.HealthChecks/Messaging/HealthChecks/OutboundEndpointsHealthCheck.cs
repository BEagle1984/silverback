// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Silverback.Util;

namespace Silverback.Messaging.HealthChecks
{
    /// <summary>
    ///     Sends a ping message to all the outbound endpoints to verify that they can all be produced to.
    /// </summary>
    public class OutboundEndpointsHealthCheck : IHealthCheck
    {
        private readonly IOutboundEndpointsHealthCheckService _service;

        /// <summary>
        ///     Initializes a new instance of the <see cref="OutboundEndpointsHealthCheck" /> class.
        /// </summary>
        /// <param name="service">
        ///     The <see cref="IOutboundEndpointsHealthCheckService" /> implementation to be used to ping the
        ///     services.
        /// </param>
        public OutboundEndpointsHealthCheck(IOutboundEndpointsHealthCheckService service)
        {
            _service = service;
        }

        /// <inheritdoc cref="IHealthCheck.CheckHealthAsync" />
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            Check.NotNull(context, nameof(context));

            var results = await _service.PingAllEndpoints();

            return results.All(r => r.IsSuccessful)
                ? new HealthCheckResult(HealthStatus.Healthy)
                : new HealthCheckResult(context.Registration.FailureStatus, GetDescription(results));
        }

        private static string GetDescription(IEnumerable<EndpointCheckResult> results) =>
            string.Join(",", results.Select(r => $"{r.EndpointName}: {r.ErrorMessage}"));
    }
}
