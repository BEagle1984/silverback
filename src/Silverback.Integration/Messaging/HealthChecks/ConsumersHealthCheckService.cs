// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Silverback.Messaging.Broker;
using Silverback.Util;

namespace Silverback.Messaging.HealthChecks
{
    /// <inheritdoc cref="IConsumersHealthCheckService" />
    public class ConsumersHealthCheckService : IConsumersHealthCheckService
    {
        private readonly IBrokerCollection _brokerCollection;

        private bool _applicationIsStopping;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ConsumersHealthCheckService" /> class.
        /// </summary>
        /// <param name="brokerCollection">
        ///     The collection containing the available brokers.
        /// </param>
        /// <param name="applicationLifetime">
        ///     The <see cref="IHostApplicationLifetime" /> used to track the application shutdown.
        /// </param>
        public ConsumersHealthCheckService(
            IBrokerCollection brokerCollection,
            IHostApplicationLifetime applicationLifetime)
        {
            _brokerCollection = Check.NotNull(brokerCollection, nameof(brokerCollection));
            Check.NotNull(applicationLifetime, nameof(applicationLifetime));

            applicationLifetime.ApplicationStopping.Register(() => _applicationIsStopping = true);
        }

        /// <inheritdoc cref="IConsumersHealthCheckService.GetDisconnectedConsumersAsync" />
        public Task<IReadOnlyCollection<IConsumer>> GetDisconnectedConsumersAsync(
            ConsumerStatus minStatus,
            TimeSpan? gracePeriod = null)
        {
            // The check is skipped when the application is shutting down, because all consumers will be
            // disconnected and since the shutdown could take a while we don't want to report the application
            // as unhealthy.
            if (_applicationIsStopping)
                return Task.FromResult((IReadOnlyCollection<IConsumer>)Array.Empty<IConsumer>());

            IReadOnlyCollection<IConsumer> disconnectedConsumers =
                _brokerCollection
                    .SelectMany(broker => GetDisconnectedConsumers(broker, minStatus, gracePeriod)).ToList();

            return Task.FromResult(disconnectedConsumers);
        }

        private static IEnumerable<IConsumer> GetDisconnectedConsumers(
            IBroker broker,
            ConsumerStatus minStatus,
            TimeSpan? gracePeriod) =>
            broker.Consumers.Where(consumer => IsDisconnected(consumer, minStatus, gracePeriod));

        private static bool IsDisconnected(
            IConsumer consumer,
            ConsumerStatus minStatus,
            TimeSpan? gracePeriod) =>
            consumer.StatusInfo.Status < minStatus &&
            (gracePeriod == null ||
             consumer.StatusInfo.History.Count == 0 ||
             GracePeriodElapsed(consumer, gracePeriod.Value));

        private static bool GracePeriodElapsed(IConsumer consumer, TimeSpan gracePeriod) =>
            consumer.StatusInfo.History.Last().Timestamp < DateTime.UtcNow.Subtract(gracePeriod);
    }
}
