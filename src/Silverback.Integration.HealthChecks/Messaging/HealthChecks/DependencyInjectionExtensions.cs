using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Silverback.Messaging.HealthChecks
{
    public static class HealthCheckBuilderExtensions
    {
        /// <summary>
        /// Adds an health check that sends a ping message to all the outbound endpoints.
        /// </summary>
        /// <param name="builder">The <see cref="IHealthChecksBuilder"/>.</param>
        /// <param name="name">The health check name. If <c>null</c> the name 'OutboundEndpoints' will be used for the name.</param>
        /// <param name="failureStatus">
        /// The <see cref="HealthStatus"/> that should be reported when the health check fails. Optional. If <c>null</c> then
        /// the default status of <see cref="HealthStatus.Unhealthy"/> will be reported.
        /// </param>
        /// <param name="tags">A list of tags that can be used to filter sets of health checks. Optional.</param>
        /// <returns>The <see cref="IHealthChecksBuilder"/>.</returns>
        public static IHealthChecksBuilder AddOutboundEndpointsCheck(this IHealthChecksBuilder builder, string name = "OutboundEndpoints", HealthStatus? failureStatus = default, IEnumerable<string> tags = default)
        {
            builder.Services.AddScoped<IOutboundEndpointsHealthCheckService, OutboundEndpointsHealthCheckService>();

            return builder.Add(new HealthCheckRegistration(
                name,
                serviceProvider =>
                    new OutboundEndpointsHealthCheck(serviceProvider
                        .GetRequiredService<IOutboundEndpointsHealthCheckService>()),
                failureStatus,
                tags));
        }

        /// <summary>
        /// Adds an health check that monitors the outbound queue (outbox table), verifying that the messages are being processed.
        /// </summary>
        /// <param name="builder">The <see cref="IHealthChecksBuilder"/>.</param>
        /// <param name="name">The health check name. If <c>null</c> the name 'OutboundQueue' will be used for the name.</param>
        /// <param name="failureStatus">
        /// The <see cref="HealthStatus"/> that should be reported when the health check fails. Optional. If <c>null</c> then
        /// the default status of <see cref="HealthStatus.Unhealthy"/> will be reported.
        /// </param>
        /// <param name="tags">A list of tags that can be used to filter sets of health checks. Optional.</param>
        /// <returns>The <see cref="IHealthChecksBuilder"/>.</returns>
        public static IHealthChecksBuilder AddOutboundQueueCheck(this IHealthChecksBuilder builder, string name = "OutboundQueue", HealthStatus? failureStatus = default, IEnumerable<string> tags = default)
        {
            builder.Services.AddScoped<IOutboundQueueHealthCheckService, OutboundQueueHealthCheckService>();

            return builder.Add(new HealthCheckRegistration(
                name,
                serviceProvider =>
                    new OutboundQueueHealthCheck(serviceProvider
                        .GetRequiredService<IOutboundQueueHealthCheckService>()),
                failureStatus,
                tags));
        }
    }
}
