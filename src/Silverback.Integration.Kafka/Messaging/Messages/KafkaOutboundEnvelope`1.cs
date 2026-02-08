// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages;

internal record KafkaOutboundEnvelope<TMessage> : OutboundEnvelope<TMessage>, IKafkaOutboundEnvelope<TMessage>, IInternalKafkaOutboundEnvelope
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
            Key = kafkaEnvelope.Key;
            RawKey = kafkaEnvelope.RawKey;
        }
    }

    public KafkaOutboundEnvelope(TMessage? message, IOutboundEnvelope clonedEnvelope, IProducer producer)
        : base(message, clonedEnvelope, producer)
    {
        if (clonedEnvelope is IKafkaOutboundEnvelope kafkaEnvelope)
        {
            Key = kafkaEnvelope.Key;
            RawKey = kafkaEnvelope.RawKey;
        }
    }

    public KafkaOffset? Offset => _offset ??= (KafkaOffset?)BrokerMessageIdentifier;

    public object? Key { get; private set; }

    public byte[]? RawKey { get; private set; }

    public string? DynamicDestinationTopic { get; private set; }

    public int? DynamicDestinationPartition { get; private set; }

    object? IKafkaOutboundEnvelope.Key => Key;

    public IKafkaOutboundEnvelope SetKey(object? key)
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
