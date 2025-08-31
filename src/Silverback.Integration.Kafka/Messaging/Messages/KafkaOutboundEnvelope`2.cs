// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Util;

namespace Silverback.Messaging.Messages;

internal record KafkaOutboundEnvelope<TMessage, TKey> : OutboundEnvelope<TMessage>, IKafkaOutboundEnvelope<TMessage, TKey>
    where TMessage : class
{
    public KafkaOutboundEnvelope(
        TMessage? message,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpointConfiguration endpointConfiguration,
        IProducer producer,
        ISilverbackContext? context = null)
        : base(message, headers, endpointConfiguration, producer, context)
    {
        Message = message;
    }

    public new TMessage? Message { get; }

    public override Type MessageType => Message?.GetType() ?? typeof(TMessage);

    public TKey? Key { get; private set; }

    object? IKafkaOutboundEnvelope.Key => Key;

    public byte[]? RawKey { get; internal set; }

    public string? DynamicDestinationTopic { get; private set; }

    public int? DynamicDestinationPartition { get; private set; }

    public IKafkaOutboundEnvelope<TMessage, TKey> SetKey(TKey? key)
    {
        Key = key;
        return this;
    }

    public IKafkaOutboundEnvelope SetDestinationTopic(string topic, int? partition = null)
    {
        DynamicDestinationTopic = Check.NotNullOrEmpty(topic, nameof(topic));
        DynamicDestinationPartition = partition;
        return this;
    }

    public IKafkaOutboundEnvelope SetDestinationPartition(int partition)
    {
        DynamicDestinationPartition = partition;
        return this;
    }
}
