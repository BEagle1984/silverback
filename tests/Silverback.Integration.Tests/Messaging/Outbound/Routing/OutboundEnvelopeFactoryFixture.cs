// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.Routing;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Outbound.Routing;

public class OutboundEnvelopeFactoryFixture
{
    [Fact]
    public void CreateEnvelope_ShouldCreateTypedEnvelope()
    {
        TestProducerEndpoint endpoint = TestProducerEndpoint.GetDefault();
        TestEventOne message = new();
        MessageHeader[] headers = { new("one", "1"), new("two", "2") };
        IProducer producer = Substitute.For<IProducer>();

        OutboundEnvelopeFactory factory = new(
            new OutboundRoutingConfiguration
            {
                PublishOutboundMessagesToInternalBus = true
            });

        IOutboundEnvelope envelope = factory.CreateEnvelope(message, headers, endpoint, producer);

        envelope.Should().BeOfType<OutboundEnvelope<TestEventOne>>();
        envelope.As<OutboundEnvelope<TestEventOne>>().Message.Should().Be(message);
        envelope.Headers.Should().ContainSingle(header => header.Name == "one" && header.Value == "1");
        envelope.Headers.Should().ContainSingle(header => header.Name == "two" && header.Value == "2");
        envelope.Endpoint.Should().Be(endpoint);
        envelope.Producer.Should().Be(producer);
    }

    [Fact]
    public void CreateEnvelope_ShouldCreateEnvelope_WhenMessageIsNull()
    {
        TestProducerEndpoint endpoint = TestProducerEndpoint.GetDefault();
        MessageHeader[] headers = { new("one", "1"), new("two", "2") };
        IProducer producer = Substitute.For<IProducer>();

        OutboundEnvelopeFactory factory = new(
            new OutboundRoutingConfiguration
            {
                PublishOutboundMessagesToInternalBus = true
            });

        IOutboundEnvelope envelope = factory.CreateEnvelope(null, headers, endpoint, producer);

        envelope.Should().BeOfType<OutboundEnvelope>();
        envelope.Message.Should().BeNull();
        envelope.Headers.Should().ContainSingle(header => header.Name == "one" && header.Value == "1");
        envelope.Headers.Should().ContainSingle(header => header.Name == "two" && header.Value == "2");
        envelope.Endpoint.Should().Be(endpoint);
        envelope.Producer.Should().Be(producer);
    }
}
