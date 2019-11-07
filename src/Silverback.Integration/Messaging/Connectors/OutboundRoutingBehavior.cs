// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Util;

namespace Silverback.Messaging.Connectors
{
    public class OutboundRoutingBehavior : ISilverbackBehavior
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IOutboundRoutingConfiguration _routing;
        private readonly IEnumerable<IOutboundConnector> _outboundConnectors;
        private readonly MessageKeyProvider _messageKeyProvider;

        public OutboundRoutingBehavior(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _routing = serviceProvider.GetRequiredService<IOutboundRoutingConfiguration>();
            _outboundConnectors = serviceProvider.GetServices<IOutboundConnector>();
            _messageKeyProvider = serviceProvider.GetRequiredService<MessageKeyProvider>();
        }

        public async Task<IEnumerable<object>> Handle(IEnumerable<object> messages, MessagesHandler next)
        {
            RelayOutboundMessages(messages);

            await WrapAndRepublishRoutedMessages(messages);

            return await next(messages);
        }

        private void RelayOutboundMessages(IEnumerable<object> messages)
        {
            messages.OfType<IOutboundMessageInternal>()
                .ForEach(outboundMessage => _outboundConnectors
                    .GetConnectorInstance(outboundMessage.Route.OutboundConnectorType)
                    .RelayMessage(outboundMessage));
        }

        private async Task WrapAndRepublishRoutedMessages(IEnumerable<object> messages)
        {
            var wrappedMessages = messages
                .Where(message => !(message is IOutboundMessageInternal))
                .SelectMany(message => _routing.GetRoutesForMessage(message)
                    .Select(route => CreateOutboundMessage(message, route)));

            if (wrappedMessages.Any())
                await _serviceProvider
                    .GetRequiredService<IPublisher>()
                    .PublishAsync(wrappedMessages);
        }
        private IOutboundMessage CreateOutboundMessage(object message, IOutboundRoute route)
        {
            var wrapper = (IOutboundMessage)Activator.CreateInstance(
                typeof(OutboundMessage<>).MakeGenericType(message.GetType()),
                message, null, route);

            _messageKeyProvider.EnsureKeyIsInitialized(wrapper);

            return wrapper;
        }
    }
}