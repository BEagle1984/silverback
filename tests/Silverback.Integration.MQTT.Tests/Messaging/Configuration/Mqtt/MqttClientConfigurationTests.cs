// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using MQTTnet.Client.Options;
using MQTTnet.Formatter;
using Silverback.Messaging;
using Silverback.Messaging.Configuration.Mqtt;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Configuration.Mqtt
{
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
            MqttClientConfiguration configuration = new ()
            {
                ChannelOptions = new MqttClientTcpOptions()
                {
                    Server = "test-server"
                },
                ClientId = string.Empty
            };

            Action act = () => configuration.Validate();

            act.Should().ThrowExactly<EndpointConfigurationException>().WithMessage("ClientId cannot be empty.");
        }

        [Fact]
        public void Validate_MissingChannelOptions_ExceptionThrown()
        {
            MqttClientConfiguration configuration = new()
            {
                ChannelOptions = null
            };

            Action act = () => configuration.Validate();

            act.Should().ThrowExactly<EndpointConfigurationException>();
        }

        [Fact]
        public void Validate_MissingTcpServer_ExceptionThrown()
        {
            MqttClientConfiguration configuration = new()
            {
                ChannelOptions = new MqttClientTcpOptions()
            };

            Action act = () => configuration.Validate();

            act.Should().ThrowExactly<EndpointConfigurationException>();
        }

        private static MqttClientConfigurationBuilder GetValidConfigBuilder() =>
            (MqttClientConfigurationBuilder)new MqttClientConfigurationBuilder().ConnectViaTcp("test-server");
    }
}
