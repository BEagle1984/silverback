// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Shouldly;
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

        configuration.RawName.ShouldBe("topic");
    }

    [Fact]
    public void RawName_ShouldReturnTopicNames_WhenSubscribedToMultipleTopics()
    {
        MqttConsumerEndpointConfiguration configuration = GetValidConfiguration() with
        {
            Topics = new ValueReadOnlyCollection<string>(["topic1", "topic2", "topic3"])
        };

        configuration.RawName.ShouldBe("topic1,topic2,topic3");
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenIsValid()
    {
        MqttConsumerEndpointConfiguration configuration = GetValidConfiguration();

        Action act = configuration.Validate;

        act.ShouldNotThrow();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenDeserializerIsNull()
    {
        MqttConsumerEndpointConfiguration configuration = GetValidConfiguration() with
        {
            Deserializer = null!
        };

        Action act = configuration.Validate;

        act.ShouldThrow<BrokerConfigurationException>();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenSequenceIsNull()
    {
        MqttConsumerEndpointConfiguration configuration = GetValidConfiguration() with
        {
            Sequence = null!
        };

        Action act = configuration.Validate;

        act.ShouldThrow<BrokerConfigurationException>();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenTopicsIsNull()
    {
        MqttConsumerEndpointConfiguration configuration = GetValidConfiguration() with
        {
            Topics = null!
        };

        Action act = configuration.Validate;

        Exception exception = act.ShouldThrow<BrokerConfigurationException>();
        exception.Message.ShouldBe("At least 1 topic must be specified.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenTopicsIsEmpty()
    {
        MqttConsumerEndpointConfiguration configuration = GetValidConfiguration() with
        {
            Topics = new ValueReadOnlyCollection<string>([])
        };

        Action act = configuration.Validate;

        Exception exception = act.ShouldThrow<BrokerConfigurationException>();
        exception.Message.ShouldBe("At least 1 topic must be specified.");
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

        Exception exception = act.ShouldThrow<BrokerConfigurationException>();
        exception.Message.ShouldBe("A topic name cannot be null or empty.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenBatchProcessingIsEnabled()
    {
        MqttConsumerEndpointConfiguration configuration = GetValidConfiguration() with
        {
            Batch = new BatchSettings { Size = 42 }
        };

        Action act = configuration.Validate;

        Exception exception = act.ShouldThrow<BrokerConfigurationException>();
        exception.Message.ShouldBe("Batch processing is currently not supported for MQTT.");
    }

    private static MqttConsumerEndpointConfiguration GetValidConfiguration() =>
        new()
        {
            Topics = new ValueReadOnlyCollection<string>(["test"])
        };
}
