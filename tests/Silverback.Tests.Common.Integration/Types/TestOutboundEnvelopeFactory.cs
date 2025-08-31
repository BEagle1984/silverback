// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
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
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpointConfiguration endpointConfiguration,
        ISilverbackContext? context = null)
        where TMessage : class =>
        new TestOutboundEnvelope<TMessage>(message, headers, endpointConfiguration, Producer);

    protected override IOutboundEnvelope CreateForNullMessage(
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpointConfiguration endpointConfiguration,
        ISilverbackContext? context = null) =>
        new TestOutboundEnvelope<object>(null, headers, endpointConfiguration, Producer);
}
