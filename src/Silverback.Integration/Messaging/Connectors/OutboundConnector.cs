// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors
{
    public class OutboundConnector : IOutboundConnector
    {
        private readonly IBroker _broker;

        public OutboundConnector(IBroker broker)
        {
            _broker = broker;
        }

        public Task RelayMessage(IOutboundMessage message) =>
            _broker.GetProducer(message.Endpoint).ProduceAsync(message.Content, message.Headers);
    }
}