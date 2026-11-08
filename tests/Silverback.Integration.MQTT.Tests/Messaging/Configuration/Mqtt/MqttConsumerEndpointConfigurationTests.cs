// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Shouldly;
using Silverback.Collections;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Sequences.Batch;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Configuration.Mqtt;

public class MqttConsumerEndpointConfigurationTests
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

    [Theory]
    [InlineData("test/+")]
    [InlineData("test/+/sub")]
    [InlineData("+")]
    [InlineData("+/+")]
    [InlineData("test/#")]
    [InlineData("#")]
    [InlineData("test//sub")]
    [InlineData("/test")]
    [InlineData("$SYS/#")]
    [InlineData("$share/group/test")]
    [InlineData("$share/group/test/+/#")]
    [InlineData("$share/group/#")]
    public void Validate_ShouldNotThrow_WhenTopicFilterIsValid(string topic)
    {
        MqttConsumerEndpointConfiguration configuration = GetValidConfiguration() with
        {
            Topics = new ValueReadOnlyCollection<string>([topic])
        };

        Action act = configuration.Validate;

        act.ShouldNotThrow();
    }

    [Theory]
    [InlineData("test/#/sub")]
    [InlineData("test/sub#")]
    [InlineData("test/#sub")]
    [InlineData("test/sub+")]
    [InlineData("test/+sub/x")]
    [InlineData("#/sub")]
    [InlineData("$share/group/test/#/sub")]
    public void Validate_ShouldThrow_WhenTopicFilterIsNotValid(string topic)
    {
        MqttConsumerEndpointConfiguration configuration = GetValidConfiguration() with
        {
            Topics = new ValueReadOnlyCollection<string>(["test", topic])
        };

        Action act = configuration.Validate;

        Exception exception = act.ShouldThrow<BrokerConfigurationException>();
        exception.Message.ShouldBe(
            $"The topic filter '{topic}' is not valid. " +
            "The wildcards '+' and '#' must occupy an entire level and '#' must be the last level.");
    }

    [Theory]
    [InlineData("$share/group")]
    [InlineData("$share/group/")]
    [InlineData("$share//test")]
    [InlineData("$share/gr+oup/test")]
    [InlineData("$share/gr#oup/test")]
    public void Validate_ShouldThrow_WhenSharedSubscriptionIsNotValid(string topic)
    {
        MqttConsumerEndpointConfiguration configuration = GetValidConfiguration() with
        {
            Topics = new ValueReadOnlyCollection<string>([topic])
        };

        Action act = configuration.Validate;

        Exception exception = act.ShouldThrow<BrokerConfigurationException>();
        exception.Message.ShouldBe(
            $"The shared subscription '{topic}' is not valid. " +
            "The expected format is '$share/{ShareName}/{TopicFilter}', with a share name without wildcards.");
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
