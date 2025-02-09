// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using MQTTnet.Protocol;
using NSubstitute;
using Shouldly;
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
        MqttConsumerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        Action act = () => builder.Build();

        act.ShouldThrow<BrokerConfigurationException>();
    }

    [Fact]
    public void ConsumeFrom_ShouldSetTopicsFromSingleTopicName()
    {
        MqttConsumerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        builder.ConsumeFrom("some-topic");

        MqttConsumerEndpointConfiguration configuration = builder.Build();
        configuration.RawName.ShouldBe("some-topic");
    }

    [Fact]
    public void ConsumeFrom_ShouldSetTopicsFromMultipleTopicNames()
    {
        MqttConsumerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        builder.ConsumeFrom("some-topic", "some-other-topic");

        MqttConsumerEndpointConfiguration configuration = builder.Build();
        configuration.Topics.ShouldBe(["some-topic", "some-other-topic"]);
    }

    [Fact]
    public void WithQualityOfServiceLevel_ShouldSetQualityOfServiceLevel()
    {
        MqttConsumerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        builder
            .ConsumeFrom("some-topic")
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce);

        MqttConsumerEndpointConfiguration configuration = builder.Build();
        configuration.QualityOfServiceLevel.ShouldBe(MqttQualityOfServiceLevel.AtLeastOnce);
    }

    [Fact]
    public void WithAtMostOnceQoS_ShouldSetQualityOfServiceLevel()
    {
        MqttConsumerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        builder
            .ConsumeFrom("some-topic")
            .WithAtMostOnceQoS();

        MqttConsumerEndpointConfiguration configuration = builder.Build();
        configuration.QualityOfServiceLevel.ShouldBe(MqttQualityOfServiceLevel.AtMostOnce);
    }

    [Fact]
    public void WithAtLeastOnceQoS_ShouldSetQualityOfServiceLevel()
    {
        MqttConsumerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        builder
            .ConsumeFrom("some-topic")
            .WithAtLeastOnceQoS();

        MqttConsumerEndpointConfiguration configuration = builder.Build();
        configuration.QualityOfServiceLevel.ShouldBe(MqttQualityOfServiceLevel.AtLeastOnce);
    }

    [Fact]
    public void WithExactlyOnceQoS_ShouldSetQualityOfServiceLevel()
    {
        MqttConsumerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        builder
            .ConsumeFrom("some-topic")
            .WithExactlyOnceQoS();

        MqttConsumerEndpointConfiguration configuration = builder.Build();
        configuration.QualityOfServiceLevel.ShouldBe(MqttQualityOfServiceLevel.ExactlyOnce);
    }
}
