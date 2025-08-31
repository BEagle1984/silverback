// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;

namespace Silverback.Messaging.Messages;

internal class KafkaOutboundEnvelopeFactory<TKey> : OutboundEnvelopeFactory
{
    public KafkaOutboundEnvelopeFactory(IProducer producer)
        : base(producer)
    {
    }

    public override IOutboundEnvelope<TMessage> Create<TMessage>(
        TMessage? message,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpointConfiguration endpointConfiguration,
        ISilverbackContext? context = null)
        where TMessage : class =>
        new KafkaOutboundEnvelope<TMessage, TKey>(
            message,
            headers,
            endpointConfiguration,
            Producer,
            context);

    protected override IOutboundEnvelope CreateForNullMessage(
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpointConfiguration endpointConfiguration,
        ISilverbackContext? context = null) =>
        new KafkaOutboundEnvelope<TKey>(
            null,
            headers,
            endpointConfiguration,
            Producer,
            context);
}
