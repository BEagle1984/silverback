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

namespace Silverback.Messaging.Connectors.Behaviors
{
    /// <summary>
    ///     Routes the messages to the outbound endpoint by wrapping them in an
    ///     <see cref="IOutboundEnvelope{TMessage}" /> that is republished to the bus.
    /// </summary>
    public class OutboundRouterBehavior : IBehavior, ISorted
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IOutboundRoutingConfiguration _routing;

        private readonly ConcurrentDictionary<IOutboundRoute, IOutboundRouter> _routers =
            new ConcurrentDictionary<IOutboundRoute, IOutboundRouter>();

        public OutboundRouterBehavior(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _routing = serviceProvider.GetRequiredService<IOutboundRoutingConfiguration>();
        }

        public async Task<IReadOnlyCollection<object>> Handle(
            IReadOnlyCollection<object> messages,
            MessagesHandler next)
        {
            var routedMessages = await WrapAndRepublishRoutedMessages(messages);

            // The routed messages are discarded because they have been republished
            // as OutboundEnvelope and they will be normally subscribable
            // (if PublishOutboundMessagesToInternalBus is true).
            messages = messages.Where(m => !routedMessages.Contains(m)).ToList();

            return await next(messages);
        }

        private async Task<IReadOnlyCollection<object>> WrapAndRepublishRoutedMessages(IEnumerable<object> messages)
        {
            var wrappedMessages = messages
                .Where(message => !(message is IOutboundEnvelope))
                .SelectMany(message =>
                    _routing
                        .GetRoutesForMessage(message)
                        .SelectMany(route =>
                            CreateOutboundEnvelope(message, route)))
                .ToList();

            if (wrappedMessages.Any())
                await _serviceProvider
                    .GetRequiredService<IPublisher>()
                    .PublishAsync(wrappedMessages);

            return wrappedMessages.Select(m => m.Message).ToList();
        }

        private IEnumerable<IOutboundEnvelope> CreateOutboundEnvelope(object message, IOutboundRoute route)
        {
            var headers = new MessageHeaderCollection();
            var router = _routers.GetOrAdd(route, _ => route.GetOutboundRouter(_serviceProvider));
            var endpoints = router.GetDestinationEndpoints(message, headers);

            foreach (var endpoint in endpoints)
            {
                yield return (IOutboundEnvelope) Activator.CreateInstance(
                    typeof(OutboundEnvelope<>).MakeGenericType(message.GetType()),
                    message,
                    headers,
                    endpoint,
                    route.OutboundConnectorType,
                    _routing.PublishOutboundMessagesToInternalBus);
            }
        }

        public int SortIndex { get; } = IntegrationBehaviorsSortIndexes.OutboundRouter;
    }
}