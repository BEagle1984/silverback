// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages;

internal record KafkaOutboundEnvelope<TMessage, TKey> : OutboundEnvelope<TMessage>, IKafkaOutboundEnvelope<TMessage, TKey>, IInternalKafkaOutboundEnvelope
    where TMessage : class
{
    private KafkaOffset? _offset;

    public KafkaOutboundEnvelope(
        TMessage? message,
        IProducer producer,
        ISilverbackContext? context = null)
        : base(message, producer, context)
    {
    }

    public KafkaOutboundEnvelope(IInboundEnvelope<TMessage> clonedEnvelope, IProducer producer, ISilverbackContext? context = null)
        : base(clonedEnvelope, producer, context)
    {
        if (clonedEnvelope is IKafkaInboundEnvelope kafkaEnvelope)
        {
            RawKey = kafkaEnvelope.RawKey;

            if (kafkaEnvelope is IKafkaInboundEnvelope<object, TKey> typedKafkaEnvelope)
                Key = typedKafkaEnvelope.Key;
        }
    }

    public KafkaOutboundEnvelope(TMessage? message, IOutboundEnvelope clonedEnvelope, IProducer producer)
        : base(message, clonedEnvelope, producer)
    {
        if (clonedEnvelope is IKafkaOutboundEnvelope kafkaEnvelope)
        {
            RawKey = kafkaEnvelope.RawKey;

            if (kafkaEnvelope is IKafkaOutboundEnvelope<object, TKey> typedKafkaEnvelope)
                Key = typedKafkaEnvelope.Key;
        }
    }

    public KafkaOffset? Offset => _offset ??= (KafkaOffset?)BrokerMessageIdentifier;

    public TKey? Key { get; private set; }

    public byte[]? RawKey { get; private set; }

    public string? DynamicDestinationTopic { get; private set; }

    public int? DynamicDestinationPartition { get; private set; }

    object? IKafkaOutboundEnvelope.Key => Key;

    public IKafkaOutboundEnvelope<TMessage, TKey> SetKey(TKey? key)
    {
        Key = key;
        return this;
    }

    public IKafkaOutboundEnvelope SetRawKey(byte[]? rawKey)
    {
        RawKey = rawKey;
        return this;
    }

    public IKafkaOutboundEnvelope SetDestinationTopic(string? topic, int? partition = null)
    {
        DynamicDestinationTopic = topic;
        DynamicDestinationPartition = partition;
        return this;
    }

    public IKafkaOutboundEnvelope SetDestinationPartition(int? partition)
    {
        DynamicDestinationPartition = partition;
        return this;
    }

    public IInternalKafkaOutboundEnvelope SetOffset(KafkaOffset? offset)
    {
        SetBrokerMessageIdentifier(offset);
        return this;
    }
}
