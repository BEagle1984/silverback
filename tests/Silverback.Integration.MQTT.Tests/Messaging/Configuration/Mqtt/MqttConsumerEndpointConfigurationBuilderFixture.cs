// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using MQTTnet.Protocol;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Configuration.Mqtt;

public class MqttConsumerEndpointConfigurationBuilderFixture
{
    [Fact]
    public void Build_ShouldThrow_WhenConfigurationIsNotValid()
    {
        MqttConsumerEndpointConfigurationBuilder<TestEventOne> builder = new();

        Action act = () => builder.Build();

        act.Should().ThrowExactly<BrokerConfigurationException>();
    }

    [Fact]
    public void ConsumeFrom_ShouldSetTopicsFromSingleTopicName()
    {
        MqttConsumerEndpointConfigurationBuilder<TestEventOne> builder = new();
        builder.ConsumeFrom("some-topic");
        MqttConsumerEndpointConfiguration configuration = builder.Build();

        configuration.RawName.Should().Be("some-topic");
    }

    [Fact]
    public void ConsumeFrom_ShouldSetTopicsFromMultipleTopicNames()
    {
        MqttConsumerEndpointConfigurationBuilder<TestEventOne> builder = new();
        builder.ConsumeFrom("some-topic", "some-other-topic");
        MqttConsumerEndpointConfiguration configuration = builder.Build();

        configuration.Topics.Should().BeEquivalentTo("some-topic", "some-other-topic");
    }

    [Fact]
    public void WithQualityOfServiceLevel_ShouldSetQualityOfServiceLevel()
    {
        MqttConsumerEndpointConfigurationBuilder<TestEventOne> builder = new();
        builder
            .ConsumeFrom("some-topic")
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce);
        MqttConsumerEndpointConfiguration configuration = builder.Build();

        configuration.QualityOfServiceLevel.Should().Be(MqttQualityOfServiceLevel.AtLeastOnce);
    }

    [Fact]
    public void WithAtMostOnceQoS_ShouldSetQualityOfServiceLevel()
    {
        MqttConsumerEndpointConfigurationBuilder<TestEventOne> builder = new();
        builder
            .ConsumeFrom("some-topic")
            .WithAtMostOnceQoS();
        MqttConsumerEndpointConfiguration configuration = builder.Build();

        configuration.QualityOfServiceLevel.Should().Be(MqttQualityOfServiceLevel.AtMostOnce);
    }

    [Fact]
    public void WithAtLeastOnceQoS_ShouldSetQualityOfServiceLevel()
    {
        MqttConsumerEndpointConfigurationBuilder<TestEventOne> builder = new();
        builder
            .ConsumeFrom("some-topic")
            .WithAtLeastOnceQoS();
        MqttConsumerEndpointConfiguration configuration = builder.Build();

        configuration.QualityOfServiceLevel.Should().Be(MqttQualityOfServiceLevel.AtLeastOnce);
    }

    [Fact]
    public void WithExactlyOnceQoS_ShouldSetQualityOfServiceLevel()
    {
        MqttConsumerEndpointConfigurationBuilder<TestEventOne> builder = new();
        builder
            .ConsumeFrom("some-topic")
            .WithExactlyOnceQoS();
        MqttConsumerEndpointConfiguration configuration = builder.Build();

        configuration.QualityOfServiceLevel.Should().Be(MqttQualityOfServiceLevel.ExactlyOnce);
    }
}
