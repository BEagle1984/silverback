// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.EnrichedMessages;
using Silverback.Messaging.Producing.Routing;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Producing.Routing;

public class OutboundEnvelopeFactoryFixture
{
    [Fact]
    public void CreateEnvelope_ShouldCreateTypedEnvelope()
    {
        TestProducerEndpoint endpoint = TestProducerEndpoint.GetDefault();
        TestEventOne message = new();
        MessageHeader[] headers = [new MessageHeader("one", "1"), new MessageHeader("two", "2")];
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
        MessageHeader[] headers = [new MessageHeader("one", "1"), new MessageHeader("two", "2")];
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

    [Fact]
    public void CreateEnvelope_ShouldUnwrapMessageWithHeaders()
    {
        TestProducerEndpoint endpoint = TestProducerEndpoint.GetDefault();
        IMessageWithHeaders message = new TestEventOne().AddHeader("one", "1").AddHeader("two", "2");
        MessageHeader[] headers = [new MessageHeader("three", "3"), new MessageHeader("four", "4")];
        IProducer producer = Substitute.For<IProducer>();

        OutboundEnvelopeFactory factory = new(
            new OutboundRoutingConfiguration
            {
                PublishOutboundMessagesToInternalBus = true
            });

        IOutboundEnvelope envelope = factory.CreateEnvelope(message, headers, endpoint, producer);

        envelope.Should().BeOfType<OutboundEnvelope<TestEventOne>>();
        envelope.As<OutboundEnvelope<TestEventOne>>().Message.Should().Be(message.Message);
        envelope.Headers.Should().ContainSingle(header => header.Name == "one" && header.Value == "1");
        envelope.Headers.Should().ContainSingle(header => header.Name == "two" && header.Value == "2");
        envelope.Headers.Should().ContainSingle(header => header.Name == "three" && header.Value == "3");
        envelope.Headers.Should().ContainSingle(header => header.Name == "four" && header.Value == "4");
        envelope.Endpoint.Should().Be(endpoint);
        envelope.Producer.Should().Be(producer);
    }

    [Fact]
    public void StaticCreateEnvelope_ShouldCreateTypedEnvelope()
    {
        TestProducerEndpoint endpoint = TestProducerEndpoint.GetDefault();
        TestEventOne message = new();
        MessageHeader[] headers = [new MessageHeader("one", "1"), new MessageHeader("two", "2")];
        IProducer producer = Substitute.For<IProducer>();

        IOutboundEnvelope envelope = OutboundEnvelopeFactory.CreateEnvelope(message, headers, endpoint, producer, null, true);

        envelope.Should().BeOfType<OutboundEnvelope<TestEventOne>>();
        envelope.As<OutboundEnvelope<TestEventOne>>().Message.Should().Be(message);
        envelope.Headers.Should().ContainSingle(header => header.Name == "one" && header.Value == "1");
        envelope.Headers.Should().ContainSingle(header => header.Name == "two" && header.Value == "2");
        envelope.Endpoint.Should().Be(endpoint);
        envelope.Producer.Should().Be(producer);
    }

    [Fact]
    public void CreateSimilarEnvelope_ShouldCreateTypedEnvelope()
    {
        TestProducerEndpoint endpoint = TestProducerEndpoint.GetDefault();
        TestEventOne message = new();
        MessageHeader[] headers = [new MessageHeader("one", "1"), new MessageHeader("two", "2")];
        IProducer producer = Substitute.For<IProducer>();
        OutboundEnvelope<TestEventOne> originalEnvelope = new(message, headers, endpoint, producer, null, true);

        IOutboundEnvelope envelope = OutboundEnvelopeFactory.CreateSimilarEnvelope(message, originalEnvelope);

        envelope.Should().BeOfType<OutboundEnvelope<TestEventOne>>();
        envelope.As<OutboundEnvelope<TestEventOne>>().Message.Should().Be(message);
        envelope.Headers.Should().ContainSingle(header => header.Name == "one" && header.Value == "1");
        envelope.Headers.Should().ContainSingle(header => header.Name == "two" && header.Value == "2");
        envelope.Endpoint.Should().Be(endpoint);
        envelope.Producer.Should().Be(producer);
    }
}
