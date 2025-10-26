// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     Serializes the message being produced using the configured <see cref="IMessageSerializer" />.
/// </summary>
public class SerializerProducerBehavior : IProducerBehavior
{
    /// <inheritdoc cref="ISorted.SortIndex" />
    public int SortIndex => BrokerBehaviorsSortIndexes.Producer.Serializer;

    /// <inheritdoc cref="IProducerBehavior.HandleAsync" />
    public async ValueTask HandleAsync(ProducerPipelineContext context, ProducerBehaviorHandler next, CancellationToken cancellationToken)
    {
        Check.NotNull(context, nameof(context));
        Check.NotNull(next, nameof(next));

        if (context.Envelope.Message is ITombstone tombstone)
        {
            context.Envelope = context.Producer.EnvelopeFactory.CloneReplacingMessage(null, tombstone.MessageType, context.Envelope);
        }
        else if (context.Envelope.RawMessage == null)
        {
            context.Envelope.SetRawMessage(
                await context.Envelope.EndpointConfiguration.Serializer.SerializeAsync(
                        context.Envelope.Message,
                        context.Envelope.Headers,
                        context.Envelope.GetEndpoint())
                    .ConfigureAwait(false));
        }

        await next(context, cancellationToken).ConfigureAwait(false);
    }
}
