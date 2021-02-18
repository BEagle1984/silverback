// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Util;

namespace Silverback.Messaging.Outbound.Routing
{
    /// <summary>
    ///     Routes the messages to the outbound endpoint by wrapping them in an
    ///     <see cref="IOutboundEnvelope{TMessage}" /> that is republished to the bus.
    /// </summary>
    public class OutboundRouterBehavior : IBehavior, ISorted
    {
        private readonly IPublisher _publisher;

        private readonly IOutboundRoutingConfiguration _routingConfiguration;

        private readonly IServiceProvider _serviceProvider;

        private readonly ConcurrentDictionary<IOutboundRoute, IOutboundRouter> _routers = new();

        /// <summary>
        ///     Initializes a new instance of the <see cref="OutboundRouterBehavior" /> class.
        /// </summary>
        /// <param name="publisher">
        ///     The <see cref="IPublisher" />.
        /// </param>
        /// <param name="routingConfiguration">
        ///     The <see cref="IOutboundRoutingConfiguration" />.
        /// </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" />.
        /// </param>
        public OutboundRouterBehavior(
            IPublisher publisher,
            IOutboundRoutingConfiguration routingConfiguration,
            IServiceProvider serviceProvider)
        {
            _publisher = Check.NotNull(publisher, nameof(publisher));
            _routingConfiguration = Check.NotNull(routingConfiguration, nameof(routingConfiguration));
            _serviceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
        }

        /// <inheritdoc cref="ISorted.SortIndex" />
        public int SortIndex { get; } = IntegrationBehaviorsSortIndexes.OutboundRouter;

        /// <inheritdoc cref="IBehavior.HandleAsync" />
        public async Task<IReadOnlyCollection<object?>> HandleAsync(object message, MessageHandler next)
        {
            Check.NotNull(next, nameof(next));

            var wasRouted = await WrapAndRepublishRoutedMessageAsync(message).ConfigureAwait(false);

            // The routed message is discarded because it has been republished
            // as OutboundEnvelope and will be normally subscribable
            // (if PublishOutboundMessagesToInternalBus is true)
            if (wasRouted)
                return Array.Empty<object?>();

            return await next(message).ConfigureAwait(false);
        }

        private async Task<bool> WrapAndRepublishRoutedMessageAsync(object message)
        {
            if (message is IOutboundEnvelope)
                return false;

            var routesCollection = _routingConfiguration.GetRoutesForMessage(message);

            if (routesCollection.Count == 0)
                return false;

            await routesCollection
                .SelectMany(route => CreateOutboundEnvelopes(message, route))
                .ForEachAsync(envelope => _publisher.PublishAsync(envelope))
                .ConfigureAwait(false);

            return true;
        }

        private IEnumerable<IOutboundEnvelope> CreateOutboundEnvelopes(object message, IOutboundRoute route)
        {
            var headers = new MessageHeaderCollection();
            var router = _routers.GetOrAdd(route, _ => route.GetOutboundRouter(_serviceProvider));
            var endpoints = router.GetDestinationEndpoints(message, headers);

            foreach (var endpoint in endpoints)
            {
                yield return (IOutboundEnvelope)Activator.CreateInstance(
                    typeof(OutboundEnvelope<>).MakeGenericType(message.GetType()),
                    message,
                    headers,
                    endpoint,
                    _routingConfiguration.PublishOutboundMessagesToInternalBus);
            }
        }
    }
}
