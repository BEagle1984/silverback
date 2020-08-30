// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Silverback.Util;

namespace Silverback.Messaging.HealthChecks
{
    /// <summary>
    ///     Verifies whether all consumers are connected.
    /// </summary>
    public class ConsumersHealthCheck : IHealthCheck
    {
        private readonly IConsumersHealthCheckService _service;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ConsumersHealthCheck" /> class.
        /// </summary>
        /// <param name="service">
        ///     The <see cref="IConsumersHealthCheckService" /> implementation to be used to check the consumers.
        /// </param>
        public ConsumersHealthCheck(IConsumersHealthCheckService service)
        {
            _service = service;
        }

        /// <inheritdoc cref="IHealthCheck.CheckHealthAsync" />
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            Check.NotNull(context, nameof(context));

            if (await _service.CheckConsumersConnectedAsync().ConfigureAwait(false))
                return new HealthCheckResult(HealthStatus.Healthy);

            string errorMessage = "One or more consumers are not connected.";

            return new HealthCheckResult(context.Registration.FailureStatus, errorMessage);
        }
    }
}
