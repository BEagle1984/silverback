// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Silverback.Messaging.HealthChecks;
using Silverback.Util;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Adds methods such as <c>AddOutboundEndpointsCheck</c> and <c>AddOutboundQueueCheck</c> to the
    ///     <see cref="IHealthChecksBuilder" />.
    /// </summary>
    public static class HealthCheckBuilderExtensions
    {
        /// <summary>
        ///     Adds an health check that sends a ping message to all the outbound endpoints.
        /// </summary>
        /// <param name="builder">
        ///     The <see cref="IHealthChecksBuilder" />.
        /// </param>
        /// <param name="name">
        ///     The health check name. If <c>null</c> the name 'OutboundEndpoints' will be used for the name.
        /// </param>
        /// <param name="failureStatus">
        ///     The <see cref="HealthStatus" /> that should be reported when the health check fails. Optional. If
        ///     <c>null</c> then the default status of <see cref="HealthStatus.Unhealthy" /> will be reported.
        /// </param>
        /// <param name="tags">
        ///     A list of tags that can be used to filter sets of health checks. Optional.
        /// </param>
        /// <returns>
        ///     The <see cref="IHealthChecksBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IHealthChecksBuilder AddOutboundEndpointsCheck(
            this IHealthChecksBuilder builder,
            string name = "OutboundEndpoints",
            HealthStatus? failureStatus = default,
            IEnumerable<string>? tags = default)
        {
            Check.NotNull(builder, nameof(builder));

            builder.Services.AddScoped<IOutboundEndpointsHealthCheckService, OutboundEndpointsHealthCheckService>();

            static IHealthCheck ServiceFactory(IServiceProvider serviceProvider) =>
                new OutboundEndpointsHealthCheck(
                    serviceProvider.GetRequiredService<IOutboundEndpointsHealthCheckService>());

            return builder.Add(
                new HealthCheckRegistration(
                    name,
                    ServiceFactory,
                    failureStatus,
                    tags));
        }

        /// <summary>
        ///     Adds an health check that monitors the outbound queue (outbox table), verifying that the messages
        ///     are being processed.
        /// </summary>
        /// <param name="builder">
        ///     The <see cref="IHealthChecksBuilder" />.
        /// </param>
        /// <param name="name">
        ///     The health check name. If <c>null</c> the name 'OutboundQueue' will be used for the name.
        /// </param>
        /// <param name="failureStatus">
        ///     The <see cref="HealthStatus" /> that should be reported when the health check fails. Optional. If
        ///     <c>null</c> then the default status of <see cref="HealthStatus.Unhealthy" /> will be reported.
        /// </param>
        /// <param name="tags">
        ///     A list of tags that can be used to filter sets of health checks. Optional.
        /// </param>
        /// <returns>
        ///     The <see cref="IHealthChecksBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IHealthChecksBuilder AddOutboundQueueCheck(
            this IHealthChecksBuilder builder,
            string name = "OutboundQueue",
            HealthStatus? failureStatus = default,
            IEnumerable<string>? tags = default)
        {
            Check.NotNull(builder, nameof(builder));

            builder.Services.AddScoped<IOutboundQueueHealthCheckService, OutboundQueueHealthCheckService>();

            static IHealthCheck ServiceFactory(IServiceProvider serviceProvider) =>
                new OutboundQueueHealthCheck(serviceProvider.GetRequiredService<IOutboundQueueHealthCheckService>());

            return builder.Add(
                new HealthCheckRegistration(
                    name,
                    ServiceFactory,
                    failureStatus,
                    tags));
        }

        /// <summary>
        ///     Adds an health check that verifies that all consumers are connected.
        /// </summary>
        /// <param name="builder">
        ///     The <see cref="IHealthChecksBuilder" />.
        /// </param>
        /// <param name="name">
        ///     The health check name. If <c>null</c> the name 'OutboundQueue' will be used for the name.
        /// </param>
        /// <param name="failureStatus">
        ///     The <see cref="HealthStatus" /> that should be reported when the health check fails. Optional. If
        ///     <c>null</c> then the default status of <see cref="HealthStatus.Unhealthy" /> will be reported.
        /// </param>
        /// <param name="tags">
        ///     A list of tags that can be used to filter sets of health checks. Optional.
        /// </param>
        /// <returns>
        ///     The <see cref="IHealthChecksBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IHealthChecksBuilder AddConsumersCheck(
            this IHealthChecksBuilder builder,
            string name = "Consumers",
            HealthStatus? failureStatus = default,
            IEnumerable<string>? tags = default)
        {
            Check.NotNull(builder, nameof(builder));

            builder.Services.AddSingleton<IConsumersHealthCheckService, ConsumersHealthCheckService>();

            static IHealthCheck ServiceFactory(IServiceProvider serviceProvider) =>
                new ConsumersHealthCheck(serviceProvider.GetRequiredService<IConsumersHealthCheckService>());

            return builder.Add(
                new HealthCheckRegistration(
                    name,
                    ServiceFactory,
                    failureStatus,
                    tags));
        }
    }
}
