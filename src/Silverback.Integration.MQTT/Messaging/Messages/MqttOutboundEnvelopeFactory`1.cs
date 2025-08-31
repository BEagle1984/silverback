// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;

namespace Silverback.Messaging.Messages;

internal class MqttOutboundEnvelopeFactory<TKey> : OutboundEnvelopeFactory
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
        new MqttOutboundEnvelope<TMessage, TKey>(
            message,
            headers,
            endpointConfiguration,
            Producer,
            context);

    protected override IOutboundEnvelope CreateForNullMessage(
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpointConfiguration endpointConfiguration,
        ISilverbackContext? context = null) =>
        new MqttOutboundEnvelope<TKey>(
            null,
            headers,
            endpointConfiguration,
            Producer,
            context);
}
