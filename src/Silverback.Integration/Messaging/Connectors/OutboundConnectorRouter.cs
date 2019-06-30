// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers;

namespace Silverback.Messaging.Connectors
{
    public class OutboundConnectorRouter : ISubscriber
    {
        private readonly IOutboundRoutingConfiguration _routing;
        private readonly IEnumerable<IOutboundConnector> _outboundConnectors;
        private readonly IPublisher _publisher;
        private readonly MessageKeyProvider _messageKeyProvider;

        public OutboundConnectorRouter(IOutboundRoutingConfiguration routingConfiguration, IEnumerable<IOutboundConnector> outboundConnectors, IPublisher publisher, MessageKeyProvider messageKeyProvider)
        {
            _routing = routingConfiguration;
            _outboundConnectors = outboundConnectors;
            _publisher = publisher;
            _messageKeyProvider = messageKeyProvider;
        }

        [Subscribe]
        public Task OnMessageReceived(object message) =>
            _publisher.PublishAsync(
                _routing.GetRoutes(message)
                    .Select(route => WrapOutboundMessage(message, route)));

        private IOutboundMessage WrapOutboundMessage(object message, IOutboundRoute route)
        {
            var wrapper = (IOutboundMessageInternal)Activator.CreateInstance(
                typeof(OutboundMessage<>).MakeGenericType(message.GetType()),
                message, null, route);

            wrapper.Headers.Add(MessageHeader.MessageIdKey,
                _messageKeyProvider.GetKey(message, false) ?? Guid.NewGuid().ToString().ToLower());

            return wrapper;
        }

        [Subscribe]
        internal Task OnOutboundMessageReceived(IOutboundMessageInternal outboundMessage) =>
            _outboundConnectors.GetConnectorInstance(outboundMessage.Route.OutboundConnectorType)
                .RelayMessage(outboundMessage);
    }
}