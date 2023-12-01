// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.HealthChecks;
using Silverback.Util;

// ReSharper disable once CheckNamespace
#pragma warning disable IDE0130
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130

/// <summary>
///     Adds methods such as <see cref="AddConsumersCheck" /> and <see cref="AddOutboxCheck" /> to the
///     <see cref="IHealthChecksBuilder" />.
/// </summary>
public static class HealthCheckBuilderExtensions
{
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
        ConsumerStatus minHealthyStatus = ConsumerStatus.Connected,
        TimeSpan? gracePeriod = null,
        string name = "Consumers",
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Services.AddSingleton<IConsumersHealthCheckService, ConsumersHealthCheckService>();

        return builder.Add(new HealthCheckRegistration(name, CreateHealthCheck, failureStatus, tags));

        IHealthCheck CreateHealthCheck(IServiceProvider serviceProvider) =>
            new ConsumersHealthCheck(
                serviceProvider.GetRequiredService<IConsumersHealthCheckService>(),
                minHealthyStatus,
                gracePeriod ?? TimeSpan.FromSeconds(30));
    }

    /// <summary>
    ///     Adds a health check that monitors the outbox, verifying that the messages are being processed.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="IHealthChecksBuilder" />.
    /// </param>
    /// <param name="maxAge">
    ///     The maximum message age, the check will fail when a message exceeds this age. The default is 30 seconds.
    /// </param>
    /// <param name="maxQueueLength">
    ///     The maximum amount of messages in the queue. The default is null, meaning unrestricted.
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
        TimeSpan? maxAge = null,
        int? maxQueueLength = null,
        string name = "Outbox",
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Services.AddScoped<IOutboxHealthCheckService, OutboxHealthCheckService>();

        return builder.Add(new HealthCheckRegistration(name, CreateHealthCheck, failureStatus, tags));

        IHealthCheck CreateHealthCheck(IServiceProvider serviceProvider) =>
            new OutboxHealthCheck(
                serviceProvider.GetRequiredService<IOutboxHealthCheckService>(),
                maxAge ?? TimeSpan.FromSeconds(30),
                maxQueueLength);
    }
}
