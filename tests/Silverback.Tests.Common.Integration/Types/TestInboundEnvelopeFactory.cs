// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.IO;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Types;

internal class TestInboundEnvelopeFactory : InboundEnvelopeFactory
{
    public TestInboundEnvelopeFactory(IConsumer consumer)
        : base(consumer)
    {
    }

    public override IInboundEnvelope<TMessage> Create<TMessage>(
        TMessage? message,
        Stream? rawMessage,
        ConsumerEndpoint endpoint,
        IBrokerMessageIdentifier brokerMessageIdentifier)
        where TMessage : class =>
        new TestInboundEnvelope<TMessage>(message, rawMessage, endpoint, Consumer, brokerMessageIdentifier);

    public override IInboundEnvelope CloneReplacingMessage<TMessage>(TMessage? message, IInboundEnvelope envelope)
        where TMessage : class =>
        new TestInboundEnvelope<TMessage>(message, envelope);
}
