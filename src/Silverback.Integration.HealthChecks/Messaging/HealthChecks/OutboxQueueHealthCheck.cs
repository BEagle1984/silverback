﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Silverback.Util;

namespace Silverback.Messaging.HealthChecks
{
    /// <summary>
    ///     Monitors the outbox, verifying that the messages are being processed.
    /// </summary>
    public class OutboxQueueHealthCheck : IHealthCheck
    {
        private readonly IOutboundQueueHealthCheckService _service;

        /// <summary>
        ///     Initializes a new instance of the <see cref="OutboxQueueHealthCheck" /> class.
        /// </summary>
        /// <param name="service">
        ///     The <see cref="IOutboundQueueHealthCheckService" /> implementation to be used to monitor the
        ///     outbound queue.
        /// </param>
        public OutboxQueueHealthCheck(IOutboundQueueHealthCheckService service)
        {
            _service = service;
        }

        /// <summary>
        ///     Gets or sets the maximum message age, the check will fail when a message exceeds this age (default
        ///     is 30 seconds).
        /// </summary>
        public static TimeSpan MaxMessageAge { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        ///     Gets or sets the maximum amount of messages in the queue. The default is <c>null</c>, meaning
        ///     unrestricted.
        /// </summary>
        public static int? MaxQueueLength { get; set; }

        /// <inheritdoc cref="IHealthCheck.CheckHealthAsync" />
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = Justifications.CatchAllExceptions)]
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            Check.NotNull(context, nameof(context));

            try
            {
                if (await _service.CheckIsHealthyAsync(MaxMessageAge).ConfigureAwait(false))
                    return new HealthCheckResult(HealthStatus.Healthy);

                string errorMessage = "The outbound queue exceeded the configured limits " +
                                      $"(max message age: {MaxMessageAge.ToString()}, " +
                                      $"max queue length: {MaxQueueLength?.ToString(CultureInfo.InvariantCulture) ?? "-"}).";

                return new HealthCheckResult(context.Registration.FailureStatus, errorMessage);
            }
            catch (Exception ex)
            {
                return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
            }
        }
    }
}
