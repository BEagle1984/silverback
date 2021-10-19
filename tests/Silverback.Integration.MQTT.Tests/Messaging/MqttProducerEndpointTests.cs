// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using MQTTnet.Protocol;
using Silverback.Messaging;
using Silverback.Messaging.Configuration.Mqtt;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging;

public class MqttProducerEndpointTests
{
    [Fact]
    public void RawName_TopicNameReturned()
    {
        MqttProducerEndpoint endpoint = new("topic", new MqttProducerConfiguration());

        endpoint.RawName.Should().Be("topic");
    }

    [Fact]
    public void Equals_SameInstance_TrueReturned()
    {
        MqttProducerEndpoint endpoint1 = new("topic", new MqttProducerConfiguration());
        MqttProducerEndpoint endpoint2 = endpoint1;

        bool result = endpoint1.Equals(endpoint2);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_SameTopic_TrueReturned()
    {
        MqttProducerEndpoint endpoint1 = new(
            "topic",
            new MqttProducerConfiguration
            {
                Client = new MqttClientConfiguration
                {
                    ClientId = "client1"
                }
            });
        MqttProducerEndpoint endpoint2 = new(
            "topic",
            new MqttProducerConfiguration
            {
                Client = new MqttClientConfiguration
                {
                    ClientId = "client1"
                }
            });

        bool result = endpoint1.Equals(endpoint2);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentTopic_FalseReturned()
    {
        MqttProducerEndpoint endpoint1 = new(
            "topic1",
            new MqttProducerConfiguration
            {
                Client = new MqttClientConfiguration
                {
                    ClientId = "client1"
                }
            });
        MqttProducerEndpoint endpoint2 = new(
            "topic2",
            new MqttProducerConfiguration
            {
                Client = new MqttClientConfiguration
                {
                    ClientId = "client1"
                }
            });

        bool result = endpoint1.Equals(endpoint2);

        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_DifferentEndpointConfiguration_FalseReturned()
    {
        MqttProducerEndpoint endpoint1 = new(
            "topic",
            new MqttProducerConfiguration
            {
                QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce
            });
        MqttProducerEndpoint endpoint2 = new(
            "topic",
            new MqttProducerConfiguration
            {
                QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce
            });

        bool result = endpoint1.Equals(endpoint2);

        result.Should().BeFalse();
    }
}
