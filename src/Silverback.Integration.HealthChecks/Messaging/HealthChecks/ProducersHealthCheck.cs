// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Silverback.Util;

namespace Silverback.Messaging.HealthChecks
{
    /// <summary>
    ///     Sends a ping message through all the configured producers to verify that they are able to produce.
    /// </summary>
    public class ProducersHealthCheck : IHealthCheck
    {
        private readonly IProducersHealthCheckService _service;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ProducersHealthCheck" /> class.
        /// </summary>
        /// <param name="service">
        ///     The <see cref="IProducersHealthCheckService" /> implementation to be used to ping the
        ///     services.
        /// </param>
        public ProducersHealthCheck(IProducersHealthCheckService service)
        {
            _service = service;
        }

        /// <inheritdoc cref="IHealthCheck.CheckHealthAsync" />
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            Check.NotNull(context, nameof(context));

            IReadOnlyCollection<EndpointCheckResult> results = await _service.SendPingMessagesAsync().ConfigureAwait(false);

            return results.All(r => r.IsSuccessful)
                ? new HealthCheckResult(HealthStatus.Healthy)
                : new HealthCheckResult(context.Registration.FailureStatus, GetDescription(results));
        }

        private static string GetDescription(IEnumerable<EndpointCheckResult> results) =>
            string.Join(",", results.Select(r => $"{r.EndpointName}: {r.ErrorMessage}"));
    }
}
