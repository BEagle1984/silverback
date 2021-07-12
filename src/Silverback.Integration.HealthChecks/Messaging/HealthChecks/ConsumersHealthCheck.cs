﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Silverback.Messaging.Broker;
using Silverback.Util;

namespace Silverback.Messaging.HealthChecks
{
    /// <summary>
    ///     Verifies whether all consumers are connected.
    /// </summary>
    public class ConsumersHealthCheck : IHealthCheck
    {
        private readonly IConsumersHealthCheckService _service;

        private readonly ConsumerStatus _minHealthyStatus;

        private readonly TimeSpan _gracePeriod;

        private readonly Func<IConsumerEndpoint, bool>? _endpointsFilter;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ConsumersHealthCheck" /> class.
        /// </summary>
        /// <param name="service">
        ///     The <see cref="IConsumersHealthCheckService" /> implementation to be used to check the consumers.
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
        public ConsumersHealthCheck(
            IConsumersHealthCheckService service,
            ConsumerStatus minHealthyStatus,
            TimeSpan gracePeriod,
            Func<IConsumerEndpoint, bool>? endpointsFilter)
        {
            _service = service;
            _minHealthyStatus = minHealthyStatus;
            _gracePeriod = gracePeriod;
            _endpointsFilter = endpointsFilter;
        }

        /// <inheritdoc cref="IHealthCheck.CheckHealthAsync" />
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            Check.NotNull(context, nameof(context));

            IReadOnlyCollection<IConsumer> disconnectedConsumers =
                await _service.GetDisconnectedConsumersAsync(_minHealthyStatus, _gracePeriod, _endpointsFilter)
                    .ConfigureAwait(false);

            if (disconnectedConsumers.Count == 0)
                return new HealthCheckResult(HealthStatus.Healthy);

            string errorMessage = disconnectedConsumers.Aggregate(
                "One or more consumers are not connected:",
                (current, consumer) =>
                    $"{current}{Environment.NewLine}- " +
                    $"[{consumer.Id}] " +
                    $"{consumer.Endpoint.DisplayName} " +
                    $"({consumer.Endpoint.GetUniqueConsumerGroupName()})");

            return new HealthCheckResult(context.Registration.FailureStatus, errorMessage);
        }
    }
}
