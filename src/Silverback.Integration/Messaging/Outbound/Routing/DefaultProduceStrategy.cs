// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Outbound.Routing
{
    internal class DefaultProduceStrategy : IProduceStrategy
    {
        private readonly IBrokerCollection _brokerCollection;

        public DefaultProduceStrategy(IBrokerCollection brokerCollection)
        {
            _brokerCollection = brokerCollection;
        }

        public Task ProduceAsync(IOutboundEnvelope envelope) =>
            _brokerCollection.GetProducer(envelope.Endpoint).ProduceAsync(envelope);
    }
}
