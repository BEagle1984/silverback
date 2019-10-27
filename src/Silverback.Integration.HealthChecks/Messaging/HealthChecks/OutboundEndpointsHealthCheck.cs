// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Silverback.Messaging.HealthChecks
{
    public class OutboundEndpointsHealthCheck : IHealthCheck
    {
        private readonly IOutboundEndpointsHealthCheckService _service;

        public OutboundEndpointsHealthCheck(IOutboundEndpointsHealthCheckService service)
        {
            _service = service;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var results = await _service.PingAllEndpoints();

            return results.All(r => r.IsSuccessful) 
                ? new HealthCheckResult(HealthStatus.Healthy) 
                : new HealthCheckResult(context.Registration.FailureStatus, GetDescription(results));
        }

        private string GetDescription(IEnumerable<EndpointCheckResult> results) => 
            string.Join(",", results.Select(r => $"{r.EndpointName}: {r.ErrorMessage}"));
    }
}
