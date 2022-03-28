// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Messaging;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Producing.EndpointResolvers;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Outbound.EndpointResolvers;

public class MqttStaticProducerEndpointResolverFixture
{
    [Fact]
    public void GetEndpoint_ShouldReturnTopicFromTopicName()
    {
        MqttStaticProducerEndpointResolver endpointResolver = new("topic");

        ProducerEndpoint endpoint = endpointResolver.GetEndpoint(null, new MqttProducerEndpointConfiguration(), null!);

        endpoint.Should().NotBeNull();
        endpoint.Should().BeOfType<MqttProducerEndpoint>();
        endpoint.As<MqttProducerEndpoint>().Topic.Should().Be("topic");
    }

    [Fact]
    public void RawName_ShouldReturnTopicName()
    {
        MqttStaticProducerEndpointResolver endpointResolver = new("topic");

        endpointResolver.RawName.Should().Be("topic");
    }

    [Fact]
    public void Constructor_ValidTopic_NoExceptionThrown()
    {
        Action act = () => _ = new MqttStaticProducerEndpointResolver("topic");

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Constructor_EmptyTopic_ExceptionThrown(string? topic)
    {
        Action act = () => _ = new MqttStaticProducerEndpointResolver(topic!);

        act.Should().Throw<ArgumentException>();
    }
}
