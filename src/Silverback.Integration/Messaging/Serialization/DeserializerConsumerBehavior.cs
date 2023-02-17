// Copyright (c) 2023 Sergio Aquilini
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
    private readonly IConsumerLogger<DeserializerConsumerBehavior> _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DeserializerConsumerBehavior" /> class.
    /// </summary>
    /// <param name="logger">
    ///     The <see cref="IConsumerLogger{TCategoryName}" />.
    /// </param>
    public DeserializerConsumerBehavior(IConsumerLogger<DeserializerConsumerBehavior> logger)
    {
        _logger = Check.NotNull(logger, nameof(logger));
    }

    /// <inheritdoc cref="ISorted.SortIndex" />
    public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.Deserializer;

    /// <inheritdoc cref="IConsumerBehavior.HandleAsync" />
    public async ValueTask HandleAsync(ConsumerPipelineContext context, ConsumerBehaviorHandler next)
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

        if (envelope is IInboundEnvelope { Message: { } } inboundEnvelope)
            return inboundEnvelope;

        (object? deserializedObject, Type deserializedType) =
            await envelope.Endpoint.Configuration.Deserializer.DeserializeAsync(envelope.RawMessage, envelope.Headers, envelope.Endpoint).ConfigureAwait(false);

        envelope.Headers.AddIfNotExists(DefaultMessageHeaders.MessageType, deserializedType.AssemblyQualifiedName);

        return deserializedObject == null
            ? HandleNullMessage(envelope, deserializedType)
            : SerializationHelper.CreateTypedInboundEnvelope(envelope, deserializedObject, deserializedType);
    }

    private static IRawInboundEnvelope? HandleNullMessage(IRawInboundEnvelope envelope, Type deserializedType) =>
        envelope.Endpoint.Configuration.NullMessageHandlingStrategy switch
        {
            NullMessageHandlingStrategy.Tombstone => CreateTombstoneEnvelope(envelope, deserializedType),
            NullMessageHandlingStrategy.Legacy => SerializationHelper.CreateTypedInboundEnvelope(envelope, null, deserializedType),
            NullMessageHandlingStrategy.Skip => null,
            _ => throw new InvalidOperationException("Unknown NullMessageHandling value.")
        };

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

        return (Tombstone)Activator.CreateInstance(tombstoneType, messageId)!;
    }
}
