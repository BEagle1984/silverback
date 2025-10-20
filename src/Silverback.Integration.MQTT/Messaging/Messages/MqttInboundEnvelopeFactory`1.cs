// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.IO;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages;

internal class MqttInboundEnvelopeFactory<TCorrelationData> : InboundEnvelopeFactory
{
    public MqttInboundEnvelopeFactory(IConsumer consumer)
        : base(consumer)
    {
    }

    public override IInboundEnvelope<TMessage> Create<TMessage>(
        TMessage? message,
        Stream? rawMessage,
        ConsumerEndpoint endpoint,
        IBrokerMessageIdentifier brokerMessageIdentifier)
        where TMessage : class =>
        new MqttInboundEnvelope<TMessage, TCorrelationData>(
            message,
            rawMessage,
            endpoint,
            Consumer,
            brokerMessageIdentifier);

    public override IInboundEnvelope CloneReplacingMessage<TMessage>(TMessage? message, IInboundEnvelope envelope)
        where TMessage : class =>
        new MqttInboundEnvelope<TMessage, TCorrelationData>(message, envelope);
}
