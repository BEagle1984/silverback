// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Producing.Routing;

public class OutboundEnvelopeFactoryTests
{
    [Fact]
    public void Create_ShouldCreateTypedEnvelope()
    {
        TestProducerEndpointConfiguration endpointConfiguration = TestProducerEndpointConfiguration.GetDefault();
        TestEventOne message = new();
        IProducer producer = Substitute.For<IProducer>();
        producer.EndpointConfiguration.Returns(endpointConfiguration);

        IOutboundEnvelope envelope = new TestOutboundEnvelopeFactory(producer).Create(message);

        envelope.ShouldBeOfType<TestOutboundEnvelope<TestEventOne>>();
        envelope.Message.ShouldBe(message);
        envelope.EndpointConfiguration.ShouldBe(endpointConfiguration);
        envelope.Producer.ShouldBe(producer);
    }

    [Fact]
    public void Create_ShouldCreateEnvelope_WhenMessageIsNull()
    {
        TestProducerEndpointConfiguration endpointConfiguration = TestProducerEndpointConfiguration.GetDefault();
        IProducer producer = Substitute.For<IProducer>();
        producer.EndpointConfiguration.Returns(endpointConfiguration);

        IOutboundEnvelope envelope = new TestOutboundEnvelopeFactory(producer).Create(null);

        envelope.ShouldBeOfType<TestOutboundEnvelope<object>>();
        envelope.Message.ShouldBeNull();
        envelope.EndpointConfiguration.ShouldBe(endpointConfiguration);
        envelope.Producer.ShouldBe(producer);
    }
}
