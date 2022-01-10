// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Silverback.Util;

namespace Silverback.Messaging.HealthChecks;

/// <summary>
///     Monitors the outbox, verifying that the messages are being processed.
/// </summary>
public class OutboxHealthCheck : IHealthCheck
{
    private readonly IOutboxHealthCheckService _service;

    /// <summary>
    ///     Initializes a new instance of the <see cref="OutboxHealthCheck" /> class.
    /// </summary>
    /// <param name="service">
    ///     The <see cref="IOutboxHealthCheckService" /> implementation to be used to monitor the outbox.
    /// </param>
    public OutboxHealthCheck(IOutboxHealthCheckService service)
    {
        _service = service;
    }

    /// <summary>
    ///     Gets or sets the maximum message age, the check will fail when a message exceeds this age (default is 30 seconds).
    /// </summary>
    public static TimeSpan MaxMessageAge { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    ///     Gets or sets the maximum amount of messages in the queue. The default is <c>null</c>, meaning unrestricted.
    /// </summary>
    public static int? MaxQueueLength { get; set; }

    /// <inheritdoc cref="IHealthCheck.CheckHealthAsync" />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        Check.NotNull(context, nameof(context));

        if (await _service.CheckIsHealthyAsync(MaxMessageAge).ConfigureAwait(false))
            return new HealthCheckResult(HealthStatus.Healthy);

        string errorMessage = "The outbox exceeded the configured limits " +
                              $"(max message age: {MaxMessageAge.ToString()}, " +
                              $"max queue length: {MaxQueueLength?.ToString(CultureInfo.InvariantCulture) ?? "-"}).";

        return new HealthCheckResult(context.Registration.FailureStatus, errorMessage);
    }
}
