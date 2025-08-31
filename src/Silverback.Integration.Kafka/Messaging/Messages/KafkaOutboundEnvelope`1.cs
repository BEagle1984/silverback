// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Util;

namespace Silverback.Messaging.Messages;

internal record KafkaOutboundEnvelope<TKey> : OutboundEnvelope, IKafkaOutboundEnvelope<TKey>
{
    public KafkaOutboundEnvelope(
        object? message,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpointConfiguration endpointConfiguration,
        IProducer producer,
        ISilverbackContext? context = null,
        IBrokerMessageIdentifier? brokerMessageIdentifier = null)
        : base(message, headers, endpointConfiguration, producer, context, brokerMessageIdentifier)
    {
    }

    object? IKafkaOutboundEnvelope.Key => Key;

    public TKey? Key { get; private set; }

    public byte[]? RawKey { get; internal set; }

    public string? DynamicDestinationTopic { get; private set; }

    public int? DynamicDestinationPartition { get; private set; }

    public IKafkaOutboundEnvelope<TKey> SetKey(TKey? key)
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
