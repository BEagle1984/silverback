// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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
        ///     The health check name. If <c>null</c> the name 'OutboundEndpoints' will be used for the name.
        /// </param>
        /// <param name="failureStatus">
        ///     The <see cref="HealthStatus" /> that should be reported when the health check reports a failure. If the
        ///     provided value is <c>null</c>, then <see cref="HealthStatus.Unhealthy" /> will be reported.
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
        ///     The health check name. If <c>null</c> the name 'OutboundQueue' will be used for the name.
        /// </param>
        /// <param name="failureStatus">
        ///     The <see cref="HealthStatus" /> that should be reported when the health check reports a failure. If the
        ///     provided value is <c>null</c>, then <see cref="HealthStatus.Unhealthy" /> will be reported.
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
        /// <param name="name">
        ///     The health check name. If <c>null</c> the name 'OutboundQueue' will be used for the name.
        /// </param>
        /// <param name="failureStatus">
        ///     The <see cref="HealthStatus" /> that should be reported when the health check reports a failure. If the
        ///     provided value is <c>null</c>, then <see cref="HealthStatus.Unhealthy" /> will be reported.
        /// </param>
        /// <param name="tags">
        ///     An optional list of tags that can be used for filtering health checks.
        /// </param>
        /// <returns>
        ///     The <see cref="IHealthChecksBuilder" /> so that additional calls can be chained.
        /// </returns>
        /// <remarks>
        ///     The <see cref="ConsumerStatus.Ready" /> is considered healthy and a default grace period of 30 seconds is
        ///     applied.
        /// </remarks>
        public static IHealthChecksBuilder AddConsumersCheck(
            this IHealthChecksBuilder builder,
            string name = "Consumers",
            HealthStatus? failureStatus = null,
            IEnumerable<string>? tags = null) =>
            AddConsumersCheck(builder, ConsumerStatus.Ready, name, failureStatus, tags);

        /// <summary>
        ///     Adds a health check that verifies that all consumers are connected.
        /// </summary>
        /// <param name="builder">
        ///     The <see cref="IHealthChecksBuilder" />.
        /// </param>
        /// <param name="minHealthyStatus">
        ///     The minimum <see cref="ConsumerStatus" /> a consumer must have to be considered healthy.
        /// </param>
        /// <param name="name">
        ///     The health check name. If <c>null</c> the name 'OutboundQueue' will be used for the name.
        /// </param>
        /// <param name="failureStatus">
        ///     The <see cref="HealthStatus" /> that should be reported when the health check reports a failure. If the
        ///     provided value is <c>null</c>, then <see cref="HealthStatus.Unhealthy" /> will be reported.
        /// </param>
        /// <param name="tags">
        ///     An optional list of tags that can be used for filtering health checks.
        /// </param>
        /// <returns>
        ///     The <see cref="IHealthChecksBuilder" /> so that additional calls can be chained.
        /// </returns>
        /// <remarks>
        ///     A default grace period of 30 seconds is applied.
        /// </remarks>
        public static IHealthChecksBuilder AddConsumersCheck(
            this IHealthChecksBuilder builder,
            ConsumerStatus minHealthyStatus,
            string name = "Consumers",
            HealthStatus? failureStatus = null,
            IEnumerable<string>? tags = null) =>
            AddConsumersCheck(
                builder,
                minHealthyStatus,
                TimeSpan.FromSeconds(30),
                name,
                failureStatus,
                tags);

        /// <summary>
        ///     Adds a health check that verifies that all consumers are connected.
        /// </summary>
        /// <param name="builder">
        ///     The <see cref="IHealthChecksBuilder" />.
        /// </param>
        /// <param name="gracePeriod">
        ///     The grace period to observe before a consumer is considered unhealthy, when its status is reverted from
        ///     fully connected (e.g. because all Kafka partitions gets revoked during a rebalance).
        /// </param>
        /// <param name="name">
        ///     The health check name. If <c>null</c> the name 'OutboundQueue' will be used for the name.
        /// </param>
        /// <param name="failureStatus">
        ///     The <see cref="HealthStatus" /> that should be reported when the health check reports a failure. If the
        ///     provided value is <c>null</c>, then <see cref="HealthStatus.Unhealthy" /> will be reported.
        /// </param>
        /// <param name="tags">
        ///     An optional list of tags that can be used for filtering health checks.
        /// </param>
        /// <returns>
        ///     The <see cref="IHealthChecksBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IHealthChecksBuilder AddConsumersCheck(
            this IHealthChecksBuilder builder,
            TimeSpan gracePeriod,
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
                    ConsumerStatus.Ready,
                    gracePeriod);
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
        ///     The grace period to observe before a consumer is considered unhealthy, when its status is reverted from
        ///     fully connected (e.g. because all Kafka partitions gets revoked during a rebalance).
        /// </param>
        /// <param name="name">
        ///     The health check name. If <c>null</c> the name 'OutboundQueue' will be used for the name.
        /// </param>
        /// <param name="failureStatus">
        ///     The <see cref="HealthStatus" /> that should be reported when the health check reports a failure. If the
        ///     provided value is <c>null</c>, then <see cref="HealthStatus.Unhealthy" /> will be reported.
        /// </param>
        /// <param name="tags">
        ///     An optional list of tags that can be used for filtering health checks.
        /// </param>
        /// <returns>
        ///     The <see cref="IHealthChecksBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IHealthChecksBuilder AddConsumersCheck(
            this IHealthChecksBuilder builder,
            ConsumerStatus minHealthyStatus,
            TimeSpan gracePeriod,
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
                    gracePeriod);
        }
    }
}
