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

namespace Silverback.Tests.Integration.Mqtt.Messaging.Configuration;

public class ErrorPolicyChainBuilderMoveToMqttTopicExtensionsTests
{
    private readonly MqttEndpointsConfigurationBuilder _endpointsConfigurationBuilder;

    public ErrorPolicyChainBuilderMoveToMqttTopicExtensionsTests()
    {
        _endpointsConfigurationBuilder = new MqttEndpointsConfigurationBuilder(Substitute.For<IServiceProvider>());

        _endpointsConfigurationBuilder.ConfigureClient(config => config.ConnectViaTcp("tests-server"));
    }

    [Fact]
    public void ThenMoveToMqttTopic_EndpointBuilder_MovePolicyCreated()
    {
        ErrorPolicyChainBuilder builder = new(_endpointsConfigurationBuilder);
        builder.ThenMoveToMqttTopic(endpoint => endpoint.ProduceTo("test-move"));
        IErrorPolicy policy = builder.Build();

        policy.Should().BeOfType<MoveMessageErrorPolicy>();
        policy.As<MoveMessageErrorPolicy>().ProducerConfiguration.RawName.Should().Be("test-move");
        policy.As<MoveMessageErrorPolicy>().ProducerConfiguration
            .As<MqttProducerConfiguration>().Client.ChannelOptions
            .As<MqttClientTcpOptions>().Server.Should().Be("tests-server");
    }

    [Fact]
    public void ThenMoveToMqttTopic_EndpointBuilderWithConfiguration_SkipPolicyCreatedAndConfigurationApplied()
    {
        ErrorPolicyChainBuilder builder = new(_endpointsConfigurationBuilder);
        builder.ThenMoveToMqttTopic(
            endpoint => endpoint.ProduceTo("test-move"),
            movePolicy => movePolicy.MaxFailedAttempts(42));
        IErrorPolicy policy = builder.Build();

        policy.Should().BeOfType<MoveMessageErrorPolicy>();
        policy.As<MoveMessageErrorPolicy>().ProducerConfiguration.RawName.Should().Be("test-move");
        policy.As<MoveMessageErrorPolicy>().MaxFailedAttemptsCount.Should().Be(42);
        policy.As<MoveMessageErrorPolicy>().ProducerConfiguration
            .As<MqttProducerConfiguration>().Client.ChannelOptions
            .As<MqttClientTcpOptions>().Server.Should().Be("tests-server");
    }
}
