// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     Deserializes the messages being consumed using the configured <see cref="IMessageSerializer" />.
/// </summary>
public class DeserializerConsumerBehavior : IConsumerBehavior
{
    private readonly IInboundLogger<DeserializerConsumerBehavior> _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DeserializerConsumerBehavior" /> class.
    /// </summary>
    /// <param name="logger">
    ///     The <see cref="IInboundLogger{TCategoryName}" />.
    /// </param>
    public DeserializerConsumerBehavior(IInboundLogger<DeserializerConsumerBehavior> logger)
    {
        _logger = Check.NotNull(logger, nameof(logger));
    }

    /// <inheritdoc cref="ISorted.SortIndex" />
    public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.Deserializer;

    /// <inheritdoc cref="IConsumerBehavior.HandleAsync" />
    public async Task HandleAsync(
        ConsumerPipelineContext context,
        ConsumerBehaviorHandler next)
    {
        Check.NotNull(context, nameof(context));
        Check.NotNull(next, nameof(next));

        IRawInboundEnvelope? newEnvelope = await DeserializeAsync(context).ConfigureAwait(false);

        if (newEnvelope == null)
        {
            _logger.LogNullMessageSkipped(context.Envelope);
            return;
        }

        context.Envelope = newEnvelope;

        await next(context).ConfigureAwait(false);
    }

    private static async Task<IRawInboundEnvelope?> DeserializeAsync(ConsumerPipelineContext context)
    {
        IRawInboundEnvelope envelope = context.Envelope;

        if (envelope is IInboundEnvelope inboundEnvelope && inboundEnvelope.Message != null)
            return inboundEnvelope;

        (object? deserializedObject, Type deserializedType) =
            await envelope.Endpoint.Configuration.Serializer.DeserializeAsync(envelope.RawMessage, envelope.Headers, envelope.Endpoint)
                .ConfigureAwait(false);

        envelope.Headers.AddIfNotExists(
            DefaultMessageHeaders.MessageType,
            deserializedType.AssemblyQualifiedName);

        return deserializedObject == null
            ? HandleNullMessage(context, envelope, deserializedType)
            : SerializationHelper.CreateTypedInboundEnvelope(
                envelope,
                deserializedObject,
                deserializedType);
    }

    private static IRawInboundEnvelope? HandleNullMessage(
        ConsumerPipelineContext context,
        IRawInboundEnvelope envelope,
        Type deserializedType)
    {
        switch (context.Consumer.Configuration.NullMessageHandlingStrategy)
        {
            case NullMessageHandlingStrategy.Tombstone:
                return CreateTombstoneEnvelope(envelope, deserializedType);
            case NullMessageHandlingStrategy.Legacy:
                return SerializationHelper.CreateTypedInboundEnvelope(
                    envelope,
                    null,
                    deserializedType);
            case NullMessageHandlingStrategy.Skip:
                return null;
            default:
                throw new InvalidOperationException("Unknown NullMessageHandling value.");
        }
    }

    private static IRawInboundEnvelope CreateTombstoneEnvelope(
        IRawInboundEnvelope envelope,
        Type deserializedType)
    {
        Tombstone tombstone = CreateTombstone(deserializedType, envelope);
        return SerializationHelper.CreateTypedInboundEnvelope(
            envelope,
            tombstone,
            tombstone.GetType());
    }

    private static Tombstone CreateTombstone(Type? deserializedType, IRawInboundEnvelope envelope)
    {
        string? messageId = envelope.Headers.GetValue(DefaultMessageHeaders.MessageId);

        if (deserializedType == null)
        {
            envelope.Headers.AddOrReplace(
                DefaultMessageHeaders.MessageType,
                typeof(Tombstone).AssemblyQualifiedName);

            return new Tombstone(messageId);
        }

        Type tombstoneType = typeof(Tombstone<>).MakeGenericType(deserializedType);

        envelope.Headers.AddOrReplace(
            DefaultMessageHeaders.MessageType,
            tombstoneType.AssemblyQualifiedName);

        return (Tombstone)Activator.CreateInstance(
            tombstoneType,
            messageId);
    }
}
