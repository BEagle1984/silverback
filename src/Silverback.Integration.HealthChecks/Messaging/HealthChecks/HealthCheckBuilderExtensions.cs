// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
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
        ///     Adds a health check that sends a ping message to all the outbound endpoints.
        /// </summary>
        /// <param name="builder">
        ///     The <see cref="IHealthChecksBuilder" />.
        /// </param>
        /// <param name="name">
        ///     The health check name. The default is "OutboundEndpoints".
        /// </param>
        /// <param name="failureStatus">
        ///     The <see cref="HealthStatus" /> that should be reported when the health check reports a failure. The
        ///     default is <see cref="HealthStatus.Unhealthy" />.
        /// </param>
        /// <param name="tags">
        ///     An optional list of tags that can be used for filtering health checks.
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

            builder.Services
                .AddScoped<IOutboundEndpointsHealthCheckService, OutboundEndpointsHealthCheckService>();

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
        ///     Adds a health check that monitors the outbox, verifying that the messages are being processed.
        /// </summary>
        /// <param name="builder">
        ///     The <see cref="IHealthChecksBuilder" />.
        /// </param>
        /// <param name="name">
        ///     The health check name. The default is "OutboundQueue".
        /// </param>
        /// <param name="failureStatus">
        ///     The <see cref="HealthStatus" /> that should be reported when the health check reports a failure. The
        ///     default is <see cref="HealthStatus.Unhealthy" />.
        /// </param>
        /// <param name="tags">
        ///     An optional list of tags that can be used for filtering health checks.
        /// </param>
        /// <returns>
        ///     The <see cref="IHealthChecksBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IHealthChecksBuilder AddOutboxCheck(
            this IHealthChecksBuilder builder,
            string name = "OutboundQueue",
            HealthStatus? failureStatus = default,
            IEnumerable<string>? tags = default)
        {
            Check.NotNull(builder, nameof(builder));

            builder.Services.AddScoped<IOutboundQueueHealthCheckService, OutboundQueueHealthCheckService>();

            static IHealthCheck ServiceFactory(IServiceProvider serviceProvider) =>
                new OutboxQueueHealthCheck(
                    serviceProvider.GetRequiredService<IOutboundQueueHealthCheckService>());

            return builder.Add(
                new HealthCheckRegistration(
                    name,
                    ServiceFactory,
                    failureStatus,
                    tags));
        }

        /// <summary>
        ///     Adds a health check that verifies that all consumers are connected.
        /// </summary>
        /// <param name="builder">
        ///     The <see cref="IHealthChecksBuilder" />.
        /// </param>
        /// <param name="minHealthyStatus">
        ///     The minimum <see cref="ConsumerStatus" /> a consumer must have to be considered healthy.
        /// </param>
        /// <param name="gracePeriod">
        ///     The grace period to observe after each status change before a consumer is considered unhealthy.
        /// </param>
        /// <param name="endpointsFilter">
        ///     An optional filter to be applied to the endpoints to be tested.
        /// </param>
        /// <param name="name">
        ///     The health check name. The default is "Consumers".
        /// </param>
        /// <param name="failureStatus">
        ///     The <see cref="HealthStatus" /> that should be reported when the health check reports a failure. The
        ///     default is <see cref="HealthStatus.Unhealthy" />.
        /// </param>
        /// <param name="tags">
        ///     An optional list of tags that can be used for filtering health checks.
        /// </param>
        /// <returns>
        ///     The <see cref="IHealthChecksBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IHealthChecksBuilder AddConsumersCheck(
            this IHealthChecksBuilder builder,
            ConsumerStatus minHealthyStatus = ConsumerStatus.Ready,
            TimeSpan? gracePeriod = null,
            Func<IConsumerEndpoint, bool>? endpointsFilter = null,
            string name = "Consumers",
            HealthStatus? failureStatus = null,
            IEnumerable<string>? tags = null)
        {
            Check.NotNull(builder, nameof(builder));

            builder.Services.AddSingleton<IConsumersHealthCheckService, ConsumersHealthCheckService>();

            return builder.Add(
                new HealthCheckRegistration(
                    name,
                    CreateService,
                    failureStatus,
                    tags));

            IHealthCheck CreateService(IServiceProvider serviceProvider)
                => new ConsumersHealthCheck(
                    serviceProvider.GetRequiredService<IConsumersHealthCheckService>(),
                    minHealthyStatus,
                    gracePeriod ?? TimeSpan.FromSeconds(30),
                    endpointsFilter);
        }
    }
}
