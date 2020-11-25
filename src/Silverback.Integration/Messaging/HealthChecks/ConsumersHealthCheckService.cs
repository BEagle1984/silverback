// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.HealthChecks
{
    /// <inheritdoc cref="IConsumersHealthCheckService" />
    public class ConsumersHealthCheckService : IConsumersHealthCheckService
    {
        private readonly IBrokerCollection _brokerCollection;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ConsumersHealthCheckService" /> class.
        /// </summary>
        /// <param name="brokerCollection">
        ///     The collection containing the available brokers.
        /// </param>
        public ConsumersHealthCheckService(IBrokerCollection brokerCollection)
        {
            _brokerCollection = brokerCollection;
        }

        /// <inheritdoc cref="IConsumersHealthCheckService.CheckConsumersConnectedAsync" />
        public Task<bool> CheckConsumersConnectedAsync() =>
            Task.FromResult(_brokerCollection.All(broker => broker.Consumers.All(consumer => consumer.IsConnected)));
    }
}
