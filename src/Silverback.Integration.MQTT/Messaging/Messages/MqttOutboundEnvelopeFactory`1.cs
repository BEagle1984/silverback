// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;

namespace Silverback.Messaging.Messages;

internal class MqttOutboundEnvelopeFactory<TCorrelationData> : OutboundEnvelopeFactory
{
    public MqttOutboundEnvelopeFactory(IProducer producer)
        : base(producer)
    {
    }

    public override IOutboundEnvelope<TMessage> Create<TMessage>(
        TMessage? message,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpointConfiguration endpointConfiguration,
        ISilverbackContext? context = null)
        where TMessage : class =>
        new MqttOutboundEnvelope<TMessage, TCorrelationData>(
            message,
            headers,
            endpointConfiguration,
            Producer,
            context);
}
