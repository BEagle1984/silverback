// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Util;

namespace Silverback.Messaging.Producing.Routing;

/// <summary>
///     Routes the messages to the producer by wrapping them in an <see cref="IOutboundEnvelope{TMessage}" /> that is republished to the bus.
/// </summary>
public class OutboundRouterBehavior : IBehavior, ISorted
{
    private readonly IPublisher _publisher;

    private readonly IProducerCollection _producers;

    private readonly IOutboundEnvelopeFactory _envelopeFactory;

    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    ///     Initializes a new instance of the <see cref="OutboundRouterBehavior" /> class.
    /// </summary>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="producers">
    ///     The <see cref="IProducerCollection" />.
    /// </param>
    /// <param name="envelopeFactory">
    ///     The <see cref="IOutboundEnvelopeFactory" />.
    /// </param>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" />.
    /// </param>
    public OutboundRouterBehavior(
        IPublisher publisher,
        IProducerCollection producers,
        IOutboundEnvelopeFactory envelopeFactory,
        IServiceProvider serviceProvider)
    {
        _publisher = Check.NotNull(publisher, nameof(publisher));
        _producers = Check.NotNull(producers, nameof(producers));
        _envelopeFactory = Check.NotNull(envelopeFactory, nameof(envelopeFactory));
        _serviceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
    }

    /// <inheritdoc cref="ISorted.SortIndex" />
    public int SortIndex => IntegrationBehaviorsSortIndexes.OutboundRouter;

    /// <inheritdoc cref="IBehavior.HandleAsync" />
    public async Task<IReadOnlyCollection<object?>> HandleAsync(object message, MessageHandler next)
    {
        Check.NotNull(next, nameof(next));

        bool wasRouted = await WrapAndRepublishRoutedMessageAsync(message).ConfigureAwait(false);

        // The routed message is discarded because it has been republished
        // as OutboundEnvelope and will be normally subscribable
        // (if PublishOutboundMessagesToInternalBus is true)
        if (wasRouted)
            return Array.Empty<object?>();

        return await next(message).ConfigureAwait(false);
    }

    // TODO: Optimize calling the producer directly?
    private async ValueTask<bool> WrapAndRepublishRoutedMessageAsync(object message)
    {
        if (message is IOutboundEnvelope)
            return false;

        IReadOnlyCollection<IProducer> producers = _producers.GetProducersForMessage(message);

        if (producers.Count == 0)
            return false;

        await producers
            .Select(producer => CreateOutboundEnvelope(message, producer))
            .ForEachAsync(envelope => _publisher.PublishAsync(envelope))
            .ConfigureAwait(false);

        return true;
    }

    private IOutboundEnvelope CreateOutboundEnvelope(object message, IProducer producer) =>
        _envelopeFactory.CreateEnvelope(
            message,
            new MessageHeaderCollection(),
            producer.EndpointConfiguration.Endpoint.GetEndpoint(message, producer.EndpointConfiguration, _serviceProvider),
            producer);
}
