// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using MQTTnet.Client.Options;
using NSubstitute;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Inbound.ErrorHandling;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Configuration
{
    public class ErrorPolicyBuilderMoveToMqttTopicExtensionsTests
    {
        private readonly IMqttEndpointsConfigurationBuilder _endpointsConfigurationBuilder;

        public ErrorPolicyBuilderMoveToMqttTopicExtensionsTests()
        {
            _endpointsConfigurationBuilder = new MqttEndpointsConfigurationBuilder(
                new EndpointsConfigurationBuilder(Substitute.For<IServiceProvider>()));

            _endpointsConfigurationBuilder.Configure(config => config.ConnectViaTcp("tests-server"));
        }

        [Fact]
        public void MoveToMqttTopic_EndpointBuilder_MovePolicyCreated()
        {
            var builder = new ErrorPolicyBuilder(_endpointsConfigurationBuilder);
            builder.MoveToMqttTopic(endpoint => endpoint.ProduceTo("test-move"));
            var policy = builder.Build();

            policy.Should().BeOfType<MoveMessageErrorPolicy>();
            policy.As<MoveMessageErrorPolicy>().Endpoint.Name.Should().Be("test-move");
            policy.As<MoveMessageErrorPolicy>().Endpoint
                .As<MqttProducerEndpoint>().Configuration.ChannelOptions
                .As<MqttClientTcpOptions>().Server.Should().Be("tests-server");
        }

        [Fact]
        public void MoveToMqttTopic_EndpointBuilderWithConfiguration_SkipPolicyCreatedAndConfigurationApplied()
        {
            var builder = new ErrorPolicyBuilder(_endpointsConfigurationBuilder);
            builder.MoveToMqttTopic(
                endpoint => endpoint.ProduceTo("test-move"),
                movePolicy => movePolicy.MaxFailedAttempts(42));
            var policy = builder.Build();

            policy.Should().BeOfType<MoveMessageErrorPolicy>();
            policy.As<MoveMessageErrorPolicy>().Endpoint.Name.Should().Be("test-move");
            policy.As<MoveMessageErrorPolicy>().MaxFailedAttemptsCount.Should().Be(42);
            policy.As<MoveMessageErrorPolicy>().Endpoint
                .As<MqttProducerEndpoint>().Configuration.ChannelOptions
                .As<MqttClientTcpOptions>().Server.Should().Be("tests-server");
        }
    }
}
