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
        MessageHeader[] headers = [new("one", "1"), new("two", "2")];
        IProducer producer = Substitute.For<IProducer>();

        IOutboundEnvelope envelope = new TestOutboundEnvelopeFactory(producer).Create(message, headers, endpointConfiguration);

        envelope.ShouldBeOfType<TestOutboundEnvelope<TestEventOne>>();
        envelope.Message.ShouldBe(message);
        envelope.Headers.ShouldContain(header => header.Name == "one" && header.Value == "1");
        envelope.Headers.ShouldContain(header => header.Name == "two" && header.Value == "2");
        envelope.EndpointConfiguration.ShouldBe(endpointConfiguration);
        envelope.Producer.ShouldBe(producer);
    }

    [Fact]
    public void Create_ShouldCreateEnvelope_WhenMessageIsNull()
    {
        TestProducerEndpointConfiguration endpointConfiguration = TestProducerEndpointConfiguration.GetDefault();
        MessageHeader[] headers = [new("one", "1"), new("two", "2")];
        IProducer producer = Substitute.For<IProducer>();

        IOutboundEnvelope envelope = new TestOutboundEnvelopeFactory(producer).Create(null, headers, endpointConfiguration);

        envelope.ShouldBeOfType<TestOutboundEnvelope<object>>();
        envelope.Message.ShouldBeNull();
        envelope.Headers.ShouldContain(header => header.Name == "one" && header.Value == "1");
        envelope.Headers.ShouldContain(header => header.Name == "two" && header.Value == "2");
        envelope.EndpointConfiguration.ShouldBe(endpointConfiguration);
        envelope.Producer.ShouldBe(producer);
    }
}
