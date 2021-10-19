// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.BinaryFiles;

/// <summary>
///     Switches to the <see cref="BinaryFileMessageSerializer" /> if the message being consumed is a binary
///     message (according to the x-message-type header).
/// </summary>
public class BinaryFileHandlerConsumerBehavior : IConsumerBehavior
{
    /// <inheritdoc cref="ISorted.SortIndex" />
    public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.BinaryFileHandler;

    /// <inheritdoc cref="IConsumerBehavior.HandleAsync" />
    public async Task HandleAsync(
        ConsumerPipelineContext context,
        ConsumerBehaviorHandler next)
    {
        Check.NotNull(context, nameof(context));
        Check.NotNull(next, nameof(next));

        context.Envelope = await HandleAsync(context.Envelope).ConfigureAwait(false);

        await next(context).ConfigureAwait(false);
    }

    private static async Task<IRawInboundEnvelope> HandleAsync(IRawInboundEnvelope envelope)
    {
        if (envelope.Endpoint.Configuration.Serializer is BinaryFileMessageSerializer ||
            envelope.Endpoint.Configuration.Serializer.GetType().IsGenericType &&
            envelope.Endpoint.Configuration.Serializer.GetType().GetGenericTypeDefinition() ==
            typeof(BinaryFileMessageSerializer<>))
        {
            return envelope;
        }

        Type? messageType = SerializationHelper.GetTypeFromHeaders(envelope.Headers, false);
        if (messageType == null || !typeof(IBinaryFileMessage).IsAssignableFrom(messageType))
            return envelope;

        (object? deserializedObject, Type deserializedType) =
            await BinaryFileMessageSerializer.Default.DeserializeAsync(envelope.RawMessage, envelope.Headers, envelope.Endpoint)
                .ConfigureAwait(false);

        // Create typed envelope for easier specific subscription
        return SerializationHelper.CreateTypedInboundEnvelope(envelope, deserializedObject, deserializedType);
    }
}
