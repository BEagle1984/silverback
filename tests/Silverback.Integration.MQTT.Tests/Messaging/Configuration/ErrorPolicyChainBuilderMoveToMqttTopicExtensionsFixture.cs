// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Inbound.ErrorHandling;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Configuration;

public class ErrorPolicyChainBuilderMoveToMqttTopicExtensionsFixture
{
    private readonly MqttEndpointsConfigurationBuilder _endpointsConfigurationBuilder;

    public ErrorPolicyChainBuilderMoveToMqttTopicExtensionsFixture()
    {
        _endpointsConfigurationBuilder = new MqttEndpointsConfigurationBuilder(Substitute.For<IServiceProvider>())
            .ConfigureClient(client => client.ConnectViaTcp("tests-server"));
    }

    [Fact]
    public void ThenMoveToMqttTopic_ShouldAddMovePolicy()
    {
        ErrorPolicyChainBuilder builder = new(_endpointsConfigurationBuilder);
        builder.ThenMoveToMqttTopic(endpoint => endpoint.ProduceTo("test-move"));
        IErrorPolicy policy = builder.Build();

        policy.Should().BeOfType<MoveMessageErrorPolicy>();
        policy.As<MoveMessageErrorPolicy>().ProducerConfiguration.RawName.Should().Be("test-move");
        policy.As<MoveMessageErrorPolicy>().ProducerConfiguration
            .As<MqttProducerConfiguration>().Client.Channel.As<MqttClientTcpConfiguration>().Server.Should().Be("tests-server");
    }

    [Fact]
    public void ThenMoveToMqttTopic_ShouldAddMovePolicyWithSpecifiedConfiguration()
    {
        ErrorPolicyChainBuilder builder = new(_endpointsConfigurationBuilder);
        builder.ThenMoveToMqttTopic(
            endpoint => endpoint.ProduceTo("test-move"),
            movePolicy => movePolicy.WithMaxRetries(42));
        IErrorPolicy policy = builder.Build();

        policy.Should().BeOfType<MoveMessageErrorPolicy>();
        policy.As<MoveMessageErrorPolicy>().ProducerConfiguration.RawName.Should().Be("test-move");
        policy.As<MoveMessageErrorPolicy>().MaxFailedAttempts.Should().Be(42);
        policy.As<MoveMessageErrorPolicy>().ProducerConfiguration
            .As<MqttProducerConfiguration>().Client.Channel.As<MqttClientTcpConfiguration>().Server.Should().Be("tests-server");
    }
}
