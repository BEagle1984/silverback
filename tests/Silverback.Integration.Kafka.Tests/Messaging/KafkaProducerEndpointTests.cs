// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging;
using Silverback.Messaging.Sequences.Chunking;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging;

public class KafkaProducerEndpointTests
{
    [Fact]
    public void RawName_TopicNameReturned()
    {
        KafkaProducerEndpoint endpoint = new("topic", 42, new KafkaProducerConfiguration());

        endpoint.RawName.Should().Be("topic");
    }

    [Fact]
    public void Equals_SameInstance_TrueReturned()
    {
        KafkaProducerEndpoint endpoint1 = new("topic", 42, new KafkaProducerConfiguration());
        KafkaProducerEndpoint endpoint2 = endpoint1;

        bool result = endpoint1.Equals(endpoint2);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_SameTopic_TrueReturned()
    {
        KafkaProducerEndpoint endpoint1 = new("topic", 42, new KafkaProducerConfiguration());
        KafkaProducerEndpoint endpoint2 = new("topic", 42, new KafkaProducerConfiguration());

        bool result = endpoint1.Equals(endpoint2);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentTopic_FalseReturned()
    {
        KafkaProducerEndpoint endpoint1 = new("topic1", 42, new KafkaProducerConfiguration());
        KafkaProducerEndpoint endpoint2 = new("topic2", 42, new KafkaProducerConfiguration());

        bool result = endpoint1.Equals(endpoint2);

        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_DifferentPartition_FalseReturned()
    {
        KafkaProducerEndpoint endpoint1 = new("topic", 1, new KafkaProducerConfiguration());
        KafkaProducerEndpoint endpoint2 = new("topic", 2, new KafkaProducerConfiguration());

        bool result = endpoint1.Equals(endpoint2);

        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_DifferentEndpointConfiguration_FalseReturned()
    {
        KafkaProducerEndpoint endpoint1 = new(
            "topic",
            42,
            new KafkaProducerConfiguration
            {
                Chunk = new ChunkSettings
                {
                    Size = 424242
                }
            });
        KafkaProducerEndpoint endpoint2 = new(
            "topic",
            42,
            new KafkaProducerConfiguration
            {
                Chunk = null
            });

        bool result = endpoint1.Equals(endpoint2);

        result.Should().BeFalse();
    }
}
