// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
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
        TestProducerEndpointConfiguration endpointConfiguration = TestProducerEndpointConfiguration.GetDefault();
        TestEventOne message = new();
        MessageHeader[] headers = [new("one", "1"), new("two", "2")];
        IProducer producer = Substitute.For<IProducer>();

        IOutboundEnvelope envelope = OutboundEnvelopeFactory.CreateEnvelope(message, headers, endpointConfiguration, producer);

        envelope.ShouldBeOfType<OutboundEnvelope<TestEventOne>>();
        envelope.Message.ShouldBe(message);
        envelope.Headers.ShouldContain(header => header.Name == "one" && header.Value == "1");
        envelope.Headers.ShouldContain(header => header.Name == "two" && header.Value == "2");
        envelope.EndpointConfiguration.ShouldBe(endpointConfiguration);
        envelope.Producer.ShouldBe(producer);
    }

    [Fact]
    public void CreateEnvelope_ShouldCreateEnvelope_WhenMessageIsNull()
    {
        TestProducerEndpointConfiguration endpointConfiguration = TestProducerEndpointConfiguration.GetDefault();
        MessageHeader[] headers = [new("one", "1"), new("two", "2")];
        IProducer producer = Substitute.For<IProducer>();

        IOutboundEnvelope envelope = OutboundEnvelopeFactory.CreateEnvelope(null, headers, endpointConfiguration, producer);

        envelope.ShouldBeOfType<OutboundEnvelope>();
        envelope.Message.ShouldBeNull();
        envelope.Headers.ShouldContain(header => header.Name == "one" && header.Value == "1");
        envelope.Headers.ShouldContain(header => header.Name == "two" && header.Value == "2");
        envelope.EndpointConfiguration.ShouldBe(endpointConfiguration);
        envelope.Producer.ShouldBe(producer);
    }

    [Fact]
    public void StaticCreateEnvelope_ShouldCreateTypedEnvelope()
    {
        TestProducerEndpointConfiguration endpointConfiguration = TestProducerEndpointConfiguration.GetDefault();
        TestEventOne message = new();
        MessageHeader[] headers = [new("one", "1"), new("two", "2")];
        IProducer producer = Substitute.For<IProducer>();

        IOutboundEnvelope envelope = OutboundEnvelopeFactory.CreateEnvelope(message, headers, endpointConfiguration, producer);

        envelope.ShouldBeOfType<OutboundEnvelope<TestEventOne>>();
        envelope.Message.ShouldBe(message);
        envelope.Headers.ShouldContain(header => header.Name == "one" && header.Value == "1");
        envelope.Headers.ShouldContain(header => header.Name == "two" && header.Value == "2");
        envelope.EndpointConfiguration.ShouldBe(endpointConfiguration);
        envelope.Producer.ShouldBe(producer);
    }

    [Fact]
    public void CreateSimilarEnvelope_ShouldCreateTypedEnvelope()
    {
        TestProducerEndpointConfiguration endpointConfiguration = TestProducerEndpointConfiguration.GetDefault();
        TestEventOne message = new();
        MessageHeader[] headers = [new("one", "1"), new("two", "2")];
        IProducer producer = Substitute.For<IProducer>();
        OutboundEnvelope<TestEventOne> originalEnvelope = new(message, headers, endpointConfiguration, producer);

        IOutboundEnvelope envelope = OutboundEnvelopeFactory.CreateEnvelope(message, originalEnvelope);

        envelope.ShouldBeOfType<OutboundEnvelope<TestEventOne>>();
        envelope.Message.ShouldBe(message);
        envelope.Headers.ShouldContain(header => header.Name == "one" && header.Value == "1");
        envelope.Headers.ShouldContain(header => header.Name == "two" && header.Value == "2");
        envelope.EndpointConfiguration.ShouldBe(endpointConfiguration);
        envelope.Producer.ShouldBe(producer);
    }
}
