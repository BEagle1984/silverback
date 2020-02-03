// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Util;

namespace Silverback.Messaging.Connectors.Behaviors
{
    public class OutboundRoutingBehavior : IBehavior, ISorted
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IOutboundRoutingConfiguration _routing;
        private readonly MessageIdProvider _messageIdProvider;

        //TODO: Cleanup (not needed anymore since implicitly unwrapping)
        //private readonly ConcurrentBag<object> _inboundMessagesCache = new ConcurrentBag<object>();

        public OutboundRoutingBehavior(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _routing = serviceProvider.GetRequiredService<IOutboundRoutingConfiguration>();
            _messageIdProvider = serviceProvider.GetRequiredService<MessageIdProvider>();
        }

        public int SortIndex { get; } = 100;

        public async Task<IReadOnlyCollection<object>> Handle(
            IReadOnlyCollection<object> messages,
            MessagesHandler next)
        {
            //messages.OfType<IInboundEnvelope>().ForEach(envelope => _inboundMessagesCache.Add(envelope.Message));

            var routedMessages = await WrapAndRepublishRoutedMessages(messages);

            if (!_routing.PublishOutboundMessagesToInternalBus)
                messages = messages.Where(m => !routedMessages.Contains(m)).ToList();

            return await next(messages);
        }

        private async Task<IEnumerable<object>> WrapAndRepublishRoutedMessages(IEnumerable<object> messages)
        {
            var wrappedMessages = messages
                //.Where(message => !(message is IOutboundEnvelope) &&
                //                  !_inboundMessagesCache.Contains(message))
                .Where(message => !(message is IOutboundEnvelope))
                .SelectMany(message =>
                    _routing
                        .GetRoutesForMessage(message)
                        .Select(route =>
                            CreateOutboundEnvelope(message, route)))
                .ToList();

            if (wrappedMessages.Any())
                await _serviceProvider
                    .GetRequiredService<IPublisher>()
                    .PublishAsync(wrappedMessages);

            return wrappedMessages.Select(m => m.Message);
        }

        private IOutboundEnvelope CreateOutboundEnvelope(object message, IOutboundRoute route)
        {
            var envelope = (IOutboundEnvelope) Activator.CreateInstance(
                typeof(OutboundEnvelope<>).MakeGenericType(message.GetType()),
                message, null, route);

            _messageIdProvider.EnsureKeyIsInitialized(envelope.Message, envelope.Headers);

            return envelope;
        }
    }
}