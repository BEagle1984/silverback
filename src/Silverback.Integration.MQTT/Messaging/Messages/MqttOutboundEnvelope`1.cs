// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Util;

namespace Silverback.Messaging.Messages;

internal record MqttOutboundEnvelope<TCorrelationData> : OutboundEnvelope, IMqttOutboundEnvelope<TCorrelationData>
{
    public MqttOutboundEnvelope(
        object? message,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpointConfiguration endpointConfiguration,
        IProducer producer,
        ISilverbackContext? context = null,
        IBrokerMessageIdentifier? brokerMessageIdentifier = null)
        : base(message, headers, endpointConfiguration, producer, context, brokerMessageIdentifier)
    {
    }

    public TCorrelationData? CorrelationData { get; private set; }

    public byte[]? RawCorrelationData { get; internal set; }

    public string? DynamicDestinationTopic { get; private set; }

    object? IMqttOutboundEnvelope.CorrelationData => CorrelationData;

    public IMqttOutboundEnvelope SetCorrelationData(TCorrelationData? correlationData)
    {
        CorrelationData = correlationData;
        return this;
    }

    public IMqttOutboundEnvelope SetDestinationTopic(string topic)
    {
        DynamicDestinationTopic = Check.NotNullOrEmpty(topic, nameof(topic));
        return this;
    }
}
