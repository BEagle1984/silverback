// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using NSubstitute;
using Shouldly;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.EndpointResolvers;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Outbound.EndpointResolvers;

public class MqttStaticProducerEndpointResolverFixture
{
    private readonly IOutboundEnvelope<TestEventOne> _envelope = new OutboundEnvelope<TestEventOne>(
        new TestEventOne(),
        null,
        new MqttProducerEndpointConfiguration(),
        Substitute.For<IProducer>());

    [Fact]
    public void GetEndpoint_ShouldReturnTopicFromTopicName()
    {
        MqttStaticProducerEndpointResolver endpointResolver = new("topic");

        ProducerEndpoint endpoint = endpointResolver.GetEndpoint(_envelope);

        endpoint.ShouldNotBeNull();
        MqttProducerEndpoint mqttEndpoint = endpoint.ShouldBeOfType<MqttProducerEndpoint>();
        mqttEndpoint.Topic.ShouldBe("topic");
    }

    [Fact]
    public void RawName_ShouldReturnTopicName()
    {
        MqttStaticProducerEndpointResolver endpointResolver = new("topic");

        endpointResolver.RawName.ShouldBe("topic");
    }

    [Fact]
    public void Constructor_ValidTopic_NoExceptionThrown()
    {
        Action act = () => _ = new MqttStaticProducerEndpointResolver("topic");

        act.ShouldNotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Constructor_EmptyTopic_ExceptionThrown(string? topic)
    {
        Action act = () => _ = new MqttStaticProducerEndpointResolver(topic!);

        act.ShouldThrow<ArgumentException>();
    }
}
