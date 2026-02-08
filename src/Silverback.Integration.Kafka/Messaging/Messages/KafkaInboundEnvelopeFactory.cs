// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages;

internal class KafkaInboundEnvelopeFactory : InboundEnvelopeFactory
{
    public KafkaInboundEnvelopeFactory(IConsumer consumer)
        : base(consumer)
    {
    }

    public override IInboundEnvelope<TMessage> Create<TMessage>(
        TMessage? message,
        Stream? rawMessage,
        ConsumerEndpoint endpoint,
        IBrokerMessageIdentifier brokerMessageIdentifier)
        where TMessage : class =>
        new KafkaInboundEnvelope<TMessage>(
            message,
            rawMessage,
            endpoint,
            Consumer,
            brokerMessageIdentifier);

    public override IInboundEnvelope CloneReplacingMessage<TMessage>(TMessage? message, IInboundEnvelope envelope)
        where TMessage : class =>
        new KafkaInboundEnvelope<TMessage>(message, envelope);
}
