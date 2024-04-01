// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Collections;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Sequences.Batch;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Configuration.Mqtt;

public class MqttConsumerEndpointConfigurationFixture
{
    [Fact]
    public void RawName_ShouldReturnTopicName_WhenSubscribedToSingleTopic()
    {
        MqttConsumerEndpointConfiguration configuration = GetValidConfiguration() with
        {
            Topics = new ValueReadOnlyCollection<string>(["topic"])
        };

        configuration.RawName.Should().Be("topic");
    }

    [Fact]
    public void RawName_ShouldReturnTopicNames_WhenSubscribedToMultipleTopics()
    {
        MqttConsumerEndpointConfiguration configuration = GetValidConfiguration() with
        {
            Topics = new ValueReadOnlyCollection<string>(["topic1", "topic2", "topic3"])
        };

        configuration.RawName.Should().Be("topic1,topic2,topic3");
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenIsValid()
    {
        MqttConsumerEndpointConfiguration configuration = GetValidConfiguration();

        Action act = configuration.Validate;

        act.Should().NotThrow<BrokerConfigurationException>();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenDeserializerIsNull()
    {
        MqttConsumerEndpointConfiguration configuration = GetValidConfiguration() with
        {
            Deserializer = null!
        };

        Action act = configuration.Validate;

        act.Should().ThrowExactly<BrokerConfigurationException>();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenSequenceIsNull()
    {
        MqttConsumerEndpointConfiguration configuration = GetValidConfiguration() with
        {
            Sequence = null!
        };

        Action act = configuration.Validate;

        act.Should().ThrowExactly<BrokerConfigurationException>();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenTopicsIsNull()
    {
        MqttConsumerEndpointConfiguration configuration = GetValidConfiguration() with
        {
            Topics = null!
        };

        Action act = configuration.Validate;

        act.Should().ThrowExactly<BrokerConfigurationException>()
            .WithMessage("At least 1 topic must be specified.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenTopicsIsEmpty()
    {
        MqttConsumerEndpointConfiguration configuration = GetValidConfiguration() with
        {
            Topics = new ValueReadOnlyCollection<string>([])
        };

        Action act = configuration.Validate;

        act.Should().ThrowExactly<BrokerConfigurationException>()
            .WithMessage("At least 1 topic must be specified.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_ShouldThrow_WhenTopicNameIsNotValid(string? topicName)
    {
        MqttConsumerEndpointConfiguration configuration = GetValidConfiguration() with
        {
            Topics = new ValueReadOnlyCollection<string>([topicName!])
        };

        Action act = configuration.Validate;

        act.Should().ThrowExactly<BrokerConfigurationException>()
            .WithMessage("A topic name cannot be null or empty.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenBatchProcessingIsEnabled()
    {
        MqttConsumerEndpointConfiguration configuration = GetValidConfiguration() with
        {
            Batch = new BatchSettings { Size = 42 }
        };

        Action act = configuration.Validate;

        act.Should().ThrowExactly<BrokerConfigurationException>()
            .WithMessage("Batch processing cannot be enabled for MQTT. This is due to the limitations of the MQTT protocol.");
    }

    private static MqttConsumerEndpointConfiguration GetValidConfiguration() =>
        new()
        {
            Topics = new ValueReadOnlyCollection<string>(["test"])
        };
}
