// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using MQTTnet.Protocol;
using Silverback.Messaging;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Configuration.Mqtt;

public class MqttConsumerConfigurationBuilderTests
{
    private readonly MqttClientConfiguration _clientConfiguration = new()
    {
        Channel = new MqttClientTcpConfiguration
        {
            Server = "tests-server"
        }
    };

    [Fact]
    public void Build_WithoutTopicName_ExceptionThrown()
    {
        MqttConsumerConfigurationBuilder<TestEventOne> builder = new(_clientConfiguration);

        Action act = () => builder.Build();

        act.Should().ThrowExactly<EndpointConfigurationException>();
    }

    [Fact]
    public void Build_WithoutServer_ExceptionThrown()
    {
        MqttConsumerConfigurationBuilder<TestEventOne> builder = new(new MqttClientConfiguration());

        Action act = () =>
        {
            builder.ConsumeFrom("some-topic");
            builder.Build();
        };

        act.Should().ThrowExactly<EndpointConfigurationException>();
    }

    [Fact]
    public void ConsumeFrom_SingleTopic_TopicSet()
    {
        MqttConsumerConfigurationBuilder<TestEventOne> builder = new(_clientConfiguration);
        builder.ConsumeFrom("some-topic");
        MqttConsumerConfiguration configuration = builder.Build();

        configuration.RawName.Should().Be("some-topic");
    }

    [Fact]
    public void ConsumeFrom_MultipleTopics_TopicsSet()
    {
        MqttConsumerConfigurationBuilder<TestEventOne> builder = new(_clientConfiguration);
        builder.ConsumeFrom("some-topic", "some-other-topic");
        MqttConsumerConfiguration configuration = builder.Build();

        configuration.Topics.Should().BeEquivalentTo("some-topic", "some-other-topic");
    }

    [Fact]
    public void Configure_BuilderAction_ConfigurationMergedWithBase()
    {
        MqttConsumerConfigurationBuilder<TestEventOne> builder = new(_clientConfiguration);
        builder
            .ConsumeFrom("some-topic")
            .ConfigureClient(configuration => configuration.WithClientId("client42"));
        MqttConsumerConfiguration configuration = builder.Build();

        configuration.Client.Channel.Should().NotBeNull();
        configuration.Client.Channel.Should().BeEquivalentTo((MqttClientTcpConfiguration?)_clientConfiguration.Channel);
        configuration.Client.ClientId.Should().Be("client42");
    }

    [Fact]
    public void WithQualityOfServiceLevel_QualityOfServiceLevelSet()
    {
        MqttConsumerConfigurationBuilder<TestEventOne> builder = new(_clientConfiguration);
        builder
            .ConsumeFrom("some-topic")
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce);
        MqttConsumerConfiguration configuration = builder.Build();

        configuration.QualityOfServiceLevel.Should().Be(MqttQualityOfServiceLevel.AtLeastOnce);
    }

    [Fact]
    public void WithAtMostOnceQoS_QualityOfServiceLevelSet()
    {
        MqttConsumerConfigurationBuilder<TestEventOne> builder = new(_clientConfiguration);
        builder
            .ConsumeFrom("some-topic")
            .WithAtMostOnceQoS();
        MqttConsumerConfiguration configuration = builder.Build();

        configuration.QualityOfServiceLevel.Should().Be(MqttQualityOfServiceLevel.AtMostOnce);
    }

    [Fact]
    public void WithAtLeastOnceQoS_QualityOfServiceLevelSet()
    {
        MqttConsumerConfigurationBuilder<TestEventOne> builder = new(_clientConfiguration);
        builder
            .ConsumeFrom("some-topic")
            .WithAtLeastOnceQoS();
        MqttConsumerConfiguration configuration = builder.Build();

        configuration.QualityOfServiceLevel.Should().Be(MqttQualityOfServiceLevel.AtLeastOnce);
    }

    [Fact]
    public void WithExactlyOnceQoS_QualityOfServiceLevelSet()
    {
        MqttConsumerConfigurationBuilder<TestEventOne> builder = new(_clientConfiguration);
        builder
            .ConsumeFrom("some-topic")
            .WithExactlyOnceQoS();
        MqttConsumerConfiguration configuration = builder.Build();

        configuration.QualityOfServiceLevel.Should().Be(MqttQualityOfServiceLevel.ExactlyOnce);
    }
}
