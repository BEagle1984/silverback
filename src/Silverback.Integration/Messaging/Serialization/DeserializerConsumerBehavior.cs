// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     Deserializes the messages being consumed using the configured <see cref="IMessageSerializer" />.
/// </summary>
public class DeserializerConsumerBehavior : IConsumerBehavior
{
    /// <inheritdoc cref="ISorted.SortIndex" />
    public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.Deserializer;

    /// <inheritdoc cref="IConsumerBehavior.HandleAsync" />
    public async ValueTask HandleAsync(ConsumerPipelineContext context, ConsumerBehaviorHandler next, CancellationToken cancellationToken)
    {
        Check.NotNull(context, nameof(context));
        Check.NotNull(next, nameof(next));

        context.Envelope = await DeserializeAsync(context).ConfigureAwait(false);

        await next(context, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IInboundEnvelope> DeserializeAsync(ConsumerPipelineContext context)
    {
        IInboundEnvelope envelope = context.Envelope;

        if (envelope.Message != null)
            return envelope;

        DeserializedMessage deserializedMessage = await envelope.Endpoint.Configuration.Deserializer.DeserializeAsync(
            envelope.RawMessage,
            envelope.Headers,
            envelope.Endpoint).ConfigureAwait(false);

        // TODO: Is this needed?
        // envelope.Headers.AddOrReplace(DefaultMessageHeaders.MessageType, deserializedMessage.MessageType.AssemblyQualifiedName);

        return context.Consumer.EnvelopeFactory.CloneReplacingMessage(deserializedMessage.Message, deserializedMessage.MessageType, envelope);
    }
}
