// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.BinaryMessages;

/// <summary>
///     Switches to the <see cref="BinaryMessageSerializer" /> if the message being consumed is a binary
///     message (according to the x-message-type header).
/// </summary>
public class BinaryMessageHandlerConsumerBehavior : IConsumerBehavior
{
    /// <inheritdoc cref="ISorted.SortIndex" />
    public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.BinaryMessageHandler;

    /// <inheritdoc cref="IConsumerBehavior.HandleAsync" />
    public async ValueTask HandleAsync(ConsumerPipelineContext context, ConsumerBehaviorHandler next, CancellationToken cancellationToken)
    {
        Check.NotNull(context, nameof(context));
        Check.NotNull(next, nameof(next));

        context.Envelope = await HandleAsync(context.Envelope).ConfigureAwait(false);

        await next(context, cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask<IRawInboundEnvelope> HandleAsync(IRawInboundEnvelope envelope)
    {
        if (envelope.Endpoint.Configuration.Deserializer is IBinaryMessageDeserializer)
            return envelope;

        Type? messageType = SerializationHelper.GetTypeFromHeaders(envelope.Headers, false);
        if (messageType == null || !typeof(IBinaryMessage).IsAssignableFrom(messageType))
            return envelope;

        (object? deserializedObject, Type deserializedType) =
            await DefaultDeserializers.Binary.DeserializeAsync(envelope.RawMessage, envelope.Headers, envelope.Endpoint).ConfigureAwait(false);

        // Create typed envelope for easier specific subscription
        return SerializationHelper.CreateTypedInboundEnvelope(envelope, deserializedObject, deserializedType);
    }
}
