// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging;

public class KafkaConsumerEndpointTests
{
    [Fact]
    public void RawName_TopicNameReturned()
    {
        KafkaConsumerEndpoint endpoint = new("topic", 42, new KafkaConsumerConfiguration());

        endpoint.RawName.Should().Be("topic");
    }

    [Fact]
    public void Equals_SameInstance_TrueReturned()
    {
        KafkaConsumerEndpoint endpoint1 = new("topic", 42, new KafkaConsumerConfiguration());
        KafkaConsumerEndpoint endpoint2 = endpoint1;

        bool result = endpoint1.Equals(endpoint2);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_SameTopic_TrueReturned()
    {
        KafkaConsumerEndpoint endpoint1 = new("topic", 42, new KafkaConsumerConfiguration());
        KafkaConsumerEndpoint endpoint2 = new("topic", 42, new KafkaConsumerConfiguration());

        bool result = endpoint1.Equals(endpoint2);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentTopic_FalseReturned()
    {
        KafkaConsumerEndpoint endpoint1 = new("topic1", 42, new KafkaConsumerConfiguration());
        KafkaConsumerEndpoint endpoint2 = new("topic2", 42, new KafkaConsumerConfiguration());

        bool result = endpoint1.Equals(endpoint2);

        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_DifferentPartition_FalseReturned()
    {
        KafkaConsumerEndpoint endpoint1 = new("topic", 1, new KafkaConsumerConfiguration());
        KafkaConsumerEndpoint endpoint2 = new("topic", 2, new KafkaConsumerConfiguration());

        bool result = endpoint1.Equals(endpoint2);

        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_DifferentEndpointConfiguration_FalseReturned()
    {
        KafkaConsumerEndpoint endpoint1 = new(
            "topic",
            42,
            new KafkaConsumerConfiguration
            {
                BackpressureLimit = 42
            });
        KafkaConsumerEndpoint endpoint2 = new(
            "topic",
            42,
            new KafkaConsumerConfiguration
            {
                ExactlyOnceStrategy = ExactlyOnceStrategy.OffsetStore()
            });

        bool result = endpoint1.Equals(endpoint2);

        result.Should().BeFalse();
    }
}
