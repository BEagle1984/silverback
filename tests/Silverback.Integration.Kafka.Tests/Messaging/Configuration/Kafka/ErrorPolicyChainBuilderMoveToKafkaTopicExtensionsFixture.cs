// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Inbound.ErrorHandling;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Configuration.Kafka;

public class ErrorPolicyChainBuilderMoveToKafkaTopicExtensionsFixture
{
    private readonly KafkaEndpointsConfigurationBuilder _endpointsConfigurationBuilder;

    public ErrorPolicyChainBuilderMoveToKafkaTopicExtensionsFixture()
    {
        _endpointsConfigurationBuilder = new KafkaEndpointsConfigurationBuilder(Substitute.For<IServiceProvider>())
            .ConfigureClient(client => client.WithBootstrapServers("PLAINTEXT://tests"));
    }

    [Fact]
    public void ThenMoveToKafkaTopic_ShouldAddMovePolicy()
    {
        ErrorPolicyChainBuilder builder = new(_endpointsConfigurationBuilder);
        builder.ThenMoveToKafkaTopic(endpoint => endpoint.ProduceTo("test-move"));
        IErrorPolicy policy = builder.Build();

        policy.Should().BeOfType<MoveMessageErrorPolicy>();
        policy.As<MoveMessageErrorPolicy>().ProducerConfiguration.RawName.Should().Be("test-move");
        policy.As<MoveMessageErrorPolicy>().ProducerConfiguration.As<KafkaProducerConfiguration>().Client.BootstrapServers
            .Should().Be("PLAINTEXT://tests");
    }

    [Fact]
    public void ThenMoveToKafkaTopic_ShouldAddMovePolicyWithSpecifiedConfiguration()
    {
        ErrorPolicyChainBuilder builder = new(_endpointsConfigurationBuilder);
        builder.ThenMoveToKafkaTopic(
            endpoint => endpoint.ProduceTo("test-move"),
            movePolicy => movePolicy.WithMaxRetries(42));
        IErrorPolicy policy = builder.Build();

        policy.Should().BeOfType<MoveMessageErrorPolicy>();
        policy.As<MoveMessageErrorPolicy>().ProducerConfiguration.RawName.Should().Be("test-move");
        policy.As<MoveMessageErrorPolicy>().MaxFailedAttempts.Should().Be(42);
        policy.As<MoveMessageErrorPolicy>().ProducerConfiguration.As<KafkaProducerConfiguration>().Client.BootstrapServers
            .Should().Be("PLAINTEXT://tests");
    }
}
