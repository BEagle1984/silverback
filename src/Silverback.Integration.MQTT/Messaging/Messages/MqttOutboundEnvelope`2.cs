// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;

namespace Silverback.Messaging.Messages;

internal record MqttOutboundEnvelope<TMessage, TCorrelationData> : OutboundEnvelope<TMessage>, IMqttOutboundEnvelope<TMessage, TCorrelationData>
    where TMessage : class
{
    public MqttOutboundEnvelope(
        TMessage? message,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpointConfiguration endpointConfiguration,
        IProducer producer,
        ISilverbackContext? context = null)
        : base(message, headers, endpointConfiguration, producer, context)
    {
    }

    public TCorrelationData? CorrelationData { get; private set; }

    public byte[]? RawCorrelationData { get; private set; }

    public string? ResponseTopic { get; private set; }

    public string? DynamicDestinationTopic { get; private set; }

    object? IMqttOutboundEnvelope.CorrelationData => CorrelationData;

    public IMqttOutboundEnvelope SetCorrelationData(TCorrelationData? correlationData)
    {
        CorrelationData = correlationData;
        return this;
    }

    public IMqttOutboundEnvelope SetRawCorrelationData(byte[]? rawCorrelationData)
    {
        RawCorrelationData = rawCorrelationData;
        return this;
    }

    public IMqttOutboundEnvelope SetResponseTopic(string? topic)
    {
        ResponseTopic = topic;
        return this;
    }

    public IMqttOutboundEnvelope SetDestinationTopic(string? topic)
    {
        DynamicDestinationTopic = topic;
        return this;
    }
}
