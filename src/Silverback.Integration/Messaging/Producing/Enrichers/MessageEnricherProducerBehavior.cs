// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Util;

namespace Silverback.Messaging.Producing.Enrichers;

/// <summary>
///     Invokes all the <see cref="IOutboundMessageEnricher" /> configured for to the endpoint.
/// </summary>
public class MessageEnricherProducerBehavior : IProducerBehavior
{
    /// <inheritdoc cref="ISorted.SortIndex" />
    public int SortIndex => BrokerBehaviorsSortIndexes.Producer.MessageEnricher;

    /// <inheritdoc cref="IProducerBehavior.HandleAsync" />
    public async ValueTask HandleAsync(ProducerPipelineContext context, ProducerBehaviorHandler next, CancellationToken cancellationToken)
    {
        Check.NotNull(context, nameof(context));
        Check.NotNull(next, nameof(next));

        foreach (IOutboundMessageEnricher enricher in context.Envelope.EndpointConfiguration.MessageEnrichers)
        {
            enricher.Enrich(context.Envelope);
        }

        await next(context, cancellationToken).ConfigureAwait(false);
    }
}
