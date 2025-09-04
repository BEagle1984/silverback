// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.IO;
using Silverback.Messaging.Broker;
using Silverback.Util;

namespace Silverback.Messaging.Messages;

internal record MqttInboundEnvelope<TMessage, TCorrelationData> : InboundEnvelope<TMessage>, IMqttInboundEnvelope<TMessage, TCorrelationData>, IInternalMqttInboundEnvelope
    where TMessage : class
{
    public MqttInboundEnvelope(
        TMessage? message,
        Stream? rawMessage,
        IReadOnlyCollection<MessageHeader>? headers,
        ConsumerEndpoint endpoint,
        IConsumer consumer,
        IBrokerMessageIdentifier brokerMessageIdentifier)
        : base(message, rawMessage, headers, endpoint, consumer, brokerMessageIdentifier)
    {
    }

    public MqttInboundEnvelope(TMessage? message, IInboundEnvelope clonedEnvelope)
        : base(message, clonedEnvelope)
    {
        IMqttInboundEnvelope clonedMqttEnvelope = Check.IsOfType<IMqttInboundEnvelope>(clonedEnvelope, nameof(clonedEnvelope));

        CorrelationData = Check.IsNullOrOfType<TCorrelationData>(clonedMqttEnvelope.CorrelationData, nameof(clonedEnvelope));
        RawCorrelationData = clonedMqttEnvelope.RawCorrelationData;
        ResponseTopic = clonedMqttEnvelope.ResponseTopic;
    }

    public TCorrelationData? CorrelationData { get; private set; }

    object? IMqttInboundEnvelope.CorrelationData => CorrelationData;

    public byte[]? RawCorrelationData { get; private set; }

    public string? ResponseTopic { get; private set; }

    public IMqttInboundEnvelope SetCorrelationData(object correlationData)
    {
        CorrelationData = Check.IsOfType<TCorrelationData>(correlationData, nameof(correlationData));
        return this;
    }

    public IMqttInboundEnvelope SetRawCorrelationData(byte[]? rawCorrelationData)
    {
        RawCorrelationData = rawCorrelationData;
        return this;
    }

    public IMqttInboundEnvelope SetResponseTopic(string? topic)
    {
        ResponseTopic = topic;
        return this;
    }
}
