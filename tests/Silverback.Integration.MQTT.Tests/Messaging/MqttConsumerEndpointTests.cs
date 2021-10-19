// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using MQTTnet.Protocol;
using Silverback.Messaging;
using Silverback.Messaging.Configuration.Mqtt;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging;

public class MqttConsumerEndpointTests
{
    [Fact]
    public void RawName_TopicNameReturned()
    {
        MqttConsumerEndpoint endpoint = new("topic", new MqttConsumerConfiguration());

        endpoint.RawName.Should().Be("topic");
    }

    [Fact]
    public void Equals_SameInstance_TrueReturned()
    {
        MqttConsumerEndpoint endpoint1 = new("topic", new MqttConsumerConfiguration());
        MqttConsumerEndpoint endpoint2 = endpoint1;

        bool result = endpoint1.Equals(endpoint2);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_SameTopic_TrueReturned()
    {
        MqttConsumerEndpoint endpoint1 = new(
            "topic",
            new MqttConsumerConfiguration
            {
                Client = new MqttClientConfiguration
                {
                    ClientId = "client1"
                }
            });
        MqttConsumerEndpoint endpoint2 = new(
            "topic",
            new MqttConsumerConfiguration
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
        MqttConsumerEndpoint endpoint1 = new(
            "topic1",
            new MqttConsumerConfiguration
            {
                Client = new MqttClientConfiguration
                {
                    ClientId = "client1"
                }
            });
        MqttConsumerEndpoint endpoint2 = new(
            "topic2",
            new MqttConsumerConfiguration
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
        MqttConsumerEndpoint endpoint1 = new(
            "topic",
            new MqttConsumerConfiguration
            {
                QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce
            });
        MqttConsumerEndpoint endpoint2 = new(
            "topic",
            new MqttConsumerConfiguration
            {
                QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce
            });

        bool result = endpoint1.Equals(endpoint2);

        result.Should().BeFalse();
    }
}
