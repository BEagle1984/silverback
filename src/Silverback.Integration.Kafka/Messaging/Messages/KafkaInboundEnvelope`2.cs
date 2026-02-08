// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using Silverback.Messaging.Broker;
using Silverback.Util;

namespace Silverback.Messaging.Messages;

internal record KafkaInboundEnvelope<TMessage> : InboundEnvelope<TMessage>, IKafkaInboundEnvelope<TMessage>, IInternalKafkaInboundEnvelope
    where TMessage : class
{
    public KafkaInboundEnvelope(
        TMessage? message,
        Stream? rawMessage,
        ConsumerEndpoint endpoint,
        IConsumer consumer,
        IBrokerMessageIdentifier brokerMessageIdentifier) // TODO: Why not declaring as KafkaOffset?
        : base(message, rawMessage, endpoint, consumer, brokerMessageIdentifier)
    {
        Offset = Check.IsOfType<KafkaOffset>(brokerMessageIdentifier, nameof(brokerMessageIdentifier));
    }

    public KafkaInboundEnvelope(TMessage? message, IInboundEnvelope clonedEnvelope)
        : base(message, clonedEnvelope)
    {
        IKafkaInboundEnvelope clonedKafkaEnvelope = Check.IsOfType<IKafkaInboundEnvelope>(clonedEnvelope, nameof(clonedEnvelope));

        Offset = clonedKafkaEnvelope.Offset;
        Key = clonedKafkaEnvelope.Key;
        RawKey = clonedKafkaEnvelope.RawKey;
        Timestamp = clonedKafkaEnvelope.Timestamp;
    }

    public KafkaOffset Offset { get; }

    public object? Key { get; private set; }

    public byte[]? RawKey { get; private set; }

    public DateTime Timestamp { get; private set; }

    object? IKafkaInboundEnvelope.Key => Key;

    public override string? GetKey() => Key?.ToString();

    public IInternalKafkaInboundEnvelope SetKey(object? key)
    {
        Key = key;
        return this;
    }

    public IInternalKafkaInboundEnvelope SetRawKey(byte[]? rawKey)
    {
        RawKey = rawKey;
        return this;
    }

    public IInternalKafkaInboundEnvelope SetTimestamp(DateTime timestamp)
    {
        Timestamp = timestamp;
        return this;
    }
}
