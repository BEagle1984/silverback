// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Types;

internal class TestOutboundEnvelopeFactory : OutboundEnvelopeFactory
{
    public TestOutboundEnvelopeFactory(IProducer producer)
        : base(producer)
    {
    }

    public override IOutboundEnvelope<TMessage> Create<TMessage>(
        TMessage? message,
        ISilverbackContext? context = null)
        where TMessage : class =>
        new TestOutboundEnvelope<TMessage>(message, Producer, context);

    public override IOutboundEnvelope<TMessage> CreateFromInboundEnvelope<TMessage>(
        IInboundEnvelope<TMessage> envelope,
        ISilverbackContext? context = null) =>
        new TestOutboundEnvelope<TMessage>(envelope, Producer, context);

    public override IOutboundEnvelope CloneReplacingMessage<TMessage>(TMessage? message, IOutboundEnvelope envelope)
        where TMessage : class =>
        new TestOutboundEnvelope<TMessage>(message, envelope, Producer);
}
