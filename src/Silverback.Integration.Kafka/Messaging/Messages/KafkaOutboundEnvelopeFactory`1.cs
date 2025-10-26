// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages;

internal class KafkaOutboundEnvelopeFactory<TKey> : OutboundEnvelopeFactory
{
    public KafkaOutboundEnvelopeFactory(IProducer producer)
        : base(producer)
    {
    }

    public override IOutboundEnvelope<TMessage> Create<TMessage>(TMessage? message, ISilverbackContext? context = null)
        where TMessage : class =>
        new KafkaOutboundEnvelope<TMessage, TKey>(message, Producer, context);

    public override IOutboundEnvelope<TMessage> CreateFromInboundEnvelope<TMessage>(
        IInboundEnvelope<TMessage> envelope,
        ISilverbackContext? context = null)
        where TMessage : class =>
        new KafkaOutboundEnvelope<TMessage, TKey>(envelope, Producer, context);

    public override IOutboundEnvelope CloneReplacingMessage<TMessage>(TMessage? message, IOutboundEnvelope envelope)
        where TMessage : class =>
        new KafkaOutboundEnvelope<TMessage, TKey>(message, envelope, Producer);
}
