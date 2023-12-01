// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
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

    private readonly TimeSpan _maxAge;

    private readonly int? _maxQueueLength;

    /// <summary>
    ///     Initializes a new instance of the <see cref="OutboxHealthCheck" /> class.
    /// </summary>
    /// <param name="service">
    ///     The <see cref="IOutboxHealthCheckService" /> implementation to be used to monitor the outbox.
    /// </param>
    /// <param name="maxAge">
    ///     The maximum message age, the check will fail when a message exceeds this age.
    /// </param>
    /// <param name="maxQueueLength">
    ///     The maximum amount of messages in the queue. The default is null, meaning unrestricted.
    /// </param>
    public OutboxHealthCheck(IOutboxHealthCheckService service, TimeSpan maxAge, int? maxQueueLength = null)
    {
        _service = service;
        _maxAge = maxAge;
        _maxQueueLength = maxQueueLength;
    }

    /// <inheritdoc cref="IHealthCheck.CheckHealthAsync" />
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception returned in the result")]
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        Check.NotNull(context, nameof(context));

        try
        {
            if (await _service.CheckIsHealthyAsync(_maxAge, _maxQueueLength).ConfigureAwait(false))
                return new HealthCheckResult(HealthStatus.Healthy);

            string errorMessage = "The outbox exceeded the configured limits " +
                                  $"(max message age: {_maxAge}, " +
                                  $"max queue length: {_maxQueueLength?.ToString(CultureInfo.InvariantCulture) ?? "-"}).";

            return new HealthCheckResult(context.Registration.FailureStatus, errorMessage);
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
        }
    }
}
