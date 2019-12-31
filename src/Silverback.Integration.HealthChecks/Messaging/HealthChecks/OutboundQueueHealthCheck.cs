// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Silverback.Messaging.HealthChecks
{
    public class OutboundQueueHealthCheck : IHealthCheck
    {
        private readonly IOutboundQueueHealthCheckService _service;

        public OutboundQueueHealthCheck(IOutboundQueueHealthCheckService service)
        {
            _service = service;
        }

        public static TimeSpan MaxMessageAge { get; set; } = TimeSpan.FromSeconds(30);
        public static int? MaxQueueLength { get; set; }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            return await _service.CheckIsHealthy(MaxMessageAge)
                ? new HealthCheckResult(HealthStatus.Healthy)
                : new HealthCheckResult(context.Registration.FailureStatus,
                    $"The outbound queue exceeded the configured limits " +
                    $"(max message age: {MaxMessageAge.ToString()}, " +
                    $"max queue length: {MaxQueueLength?.ToString() ?? "-"}).");
        }
    }
}