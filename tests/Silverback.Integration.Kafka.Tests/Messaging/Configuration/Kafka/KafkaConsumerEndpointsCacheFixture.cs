// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using Confluent.Kafka;
using FluentAssertions;
using NSubstitute;
using Silverback.Collections;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Serialization;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Configuration.Kafka;

public class KafkaConsumerEndpointsCacheFixture
{
    [Fact]
    public void GetEndpoint_ShouldReturnEndpointAndSerializer()
    {
        IKafkaMessageDeserializer deserializer = Substitute.For<IKafkaMessageDeserializer>();

        KafkaConsumerConfiguration configuration = new()
        {
            Endpoints = new[]
            {
                new KafkaConsumerEndpointConfiguration
                {
                    TopicPartitions = new[]
                    {
                        new TopicPartitionOffset("topic1", Partition.Any, Offset.Beginning)
                    }.AsValueReadOnlyCollection(),
                    Deserializer = deserializer
                }
            }.AsValueReadOnlyCollection(),
        };

        KafkaConsumerEndpointsCache cache = new(configuration);

        KafkaConsumerEndpointsCache.CachedEndpoint result = cache.GetEndpoint(new TopicPartition("topic1", 42));

        result.Endpoint.Configuration.Should().Be(configuration.Endpoints.First());
        result.Deserializer.Should().Be(deserializer);
    }

    [Fact]
    public void GetEndpoint_ShouldReturnEndpointConfiguredForAnyPartition()
    {
        KafkaConsumerConfiguration configuration = new()
        {
            Endpoints = new[]
            {
                new KafkaConsumerEndpointConfiguration
                {
                    TopicPartitions = new[]
                    {
                        new TopicPartitionOffset("topic1", Partition.Any, Offset.Beginning),
                        new TopicPartitionOffset("topic2", Partition.Any, Offset.Beginning)
                    }.AsValueReadOnlyCollection()
                }
            }.AsValueReadOnlyCollection()
        };

        KafkaConsumerEndpointsCache cache = new(configuration);

        KafkaConsumerEndpointsCache.CachedEndpoint result = cache.GetEndpoint(new TopicPartition("topic2", 42));

        result.Endpoint.TopicPartition.Should().Be(new TopicPartition("topic2", Partition.Any));
    }

    [Fact]
    public void GetEndpoint_ShouldReturnEndpointConfiguredForSpecificPartition()
    {
        KafkaConsumerConfiguration configuration = new()
        {
            Endpoints = new[]
            {
                new KafkaConsumerEndpointConfiguration
                {
                    TopicPartitions = new[]
                    {
                        new TopicPartitionOffset("topic1", 13, Offset.Beginning),
                        new TopicPartitionOffset("topic1", 42, Offset.Beginning),
                        new TopicPartitionOffset("topic2", Partition.Any, Offset.Beginning)
                    }.AsValueReadOnlyCollection()
                }
            }.AsValueReadOnlyCollection()
        };

        KafkaConsumerEndpointsCache cache = new(configuration);

        KafkaConsumerEndpointsCache.CachedEndpoint result = cache.GetEndpoint(new TopicPartition("topic1", 42));

        result.Endpoint.TopicPartition.Should().Be(new TopicPartition("topic1", 42));
    }

    [Fact]
    public void GetEndpoint_ShouldThrow_WhenNotFound()
    {
        KafkaConsumerConfiguration configuration = new()
        {
            Endpoints = new[]
            {
                new KafkaConsumerEndpointConfiguration
                {
                    TopicPartitions = new[]
                    {
                        new TopicPartitionOffset("topic1", Partition.Any, Offset.Beginning),
                        new TopicPartitionOffset("topic2", Partition.Any, Offset.Beginning)
                    }.AsValueReadOnlyCollection()
                }
            }.AsValueReadOnlyCollection()
        };

        KafkaConsumerEndpointsCache cache = new(configuration);

        Action act = () => cache.GetEndpoint(new TopicPartition("topic3", 42));

        act.Should().Throw<InvalidOperationException>().WithMessage("No configuration found for the specified topic partition 'topic3[42]'.");
    }
}
