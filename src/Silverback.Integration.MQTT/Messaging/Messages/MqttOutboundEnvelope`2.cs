// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Util;

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
        Message = message;
    }

    public new TMessage? Message { get; }

    public override Type MessageType => Message?.GetType() ?? typeof(TMessage);

    public TCorrelationData? CorrelationData { get; private set; }

    object? IMqttOutboundEnvelope.CorrelationData => CorrelationData;

    public byte[]? RawCorrelationData { get; internal set; }

    public string? DynamicDestinationTopic { get; private set; }

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
