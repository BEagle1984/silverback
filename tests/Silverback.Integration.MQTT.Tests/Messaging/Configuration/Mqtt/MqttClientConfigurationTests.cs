// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using FluentAssertions;
using MQTTnet.Formatter;
using Silverback.Collections;
using Silverback.Messaging;
using Silverback.Messaging.Configuration.Mqtt;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Configuration.Mqtt;

public class MqttClientConfigurationTests
{
    [Fact]
    public void Default_ProtocolVersionV500Set()
    {
        MqttClientConfiguration configuration = new();

        configuration.ProtocolVersion.Should().Be(MqttProtocolVersion.V500);
    }

    [Fact]
    public void Validate_ValidConfiguration_NoExceptionThrown()
    {
        MqttClientConfiguration configuration = GetValidConfigBuilder().Build();

        Action act = () => configuration.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_MissingClientId_ExceptionThrown()
    {
        MqttClientConfiguration configuration = new()
        {
            Channel = new MqttClientTcpConfiguration
            {
                Server = "test-server"
            },
            ClientId = string.Empty
        };

        Action act = () => configuration.Validate();

        act.Should().ThrowExactly<EndpointConfigurationException>()
            .WithMessage("A ClientId is required to connect with the message broker.*");
    }

    [Fact]
    public void Validate_MissingChannelConfiguration_ExceptionThrown()
    {
        MqttClientConfiguration configuration = new()
        {
            Channel = null
        };

        Action act = () => configuration.Validate();

        act.Should().ThrowExactly<EndpointConfigurationException>()
            .WithMessage("The channel configuration is required*");
    }

    [Fact]
    public void Validate_MissingTcpServer_ExceptionThrown()
    {
        MqttClientConfiguration configuration = new()
        {
            Channel = new MqttClientTcpConfiguration()
        };

        Action act = () => configuration.Validate();

        act.Should().ThrowExactly<EndpointConfigurationException>()
            .WithMessage("The server is required*");
    }

    [Fact]
    public void Validate_MissingTopicForLastWillMessage_ExceptionThrown()
    {
        MqttClientConfiguration configuration = GetValidConfigBuilder()
            .SendLastWillMessage<object>(
                _ =>
                {
                })
            .Build();

        Action act = () => configuration.Validate();

        act.Should().ThrowExactly<EndpointConfigurationException>()
            .WithMessage("The topic is required*");
    }

    [Fact]
    public void GetMqttClientOptions_ShouldReturnUserProperties()
    {
        List<MqttUserProperty> mqttUserProperties = new()
        {
            new MqttUserProperty("key1", "value1"),
            new MqttUserProperty("key2", "value2")
        };

        MqttClientConfiguration configuration = new()
        {
            UserProperties = mqttUserProperties.AsValueReadOnlyCollection()
        };

        configuration.GetMqttClientOptions().UserProperties.Should().BeEquivalentTo(mqttUserProperties);
    }

    private static MqttClientConfigurationBuilder GetValidConfigBuilder() =>
        new MqttClientConfigurationBuilder().ConnectViaTcp("test-server");
}
