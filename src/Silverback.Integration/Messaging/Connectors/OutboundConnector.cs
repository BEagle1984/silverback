// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors
{
    public class OutboundConnector : IOutboundConnector
    {
        private readonly IBrokerCollection _brokerCollection;

        public OutboundConnector(IBrokerCollection brokerCollection)
        {
            _brokerCollection = brokerCollection;
        }

        public Task RelayMessage(IOutboundEnvelope envelope) =>
            _brokerCollection.GetProducer(envelope.Endpoint).ProduceAsync(envelope);
    }
}