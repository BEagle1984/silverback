// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using Confluent.Kafka;
using Shouldly;
using Silverback.Collections;
using Silverback.Messaging;
using Silverback.Messaging.Configuration.Kafka;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Configuration.Kafka;

public class KafkaConsumerEndpointsCacheFixture
{
    [Fact]
    public void GetEndpoint_ShouldReturnEndpointAndSerializer()
    {
        KafkaConsumerConfiguration configuration = new()
        {
            Endpoints = new[]
            {
                new KafkaConsumerEndpointConfiguration
                {
                    TopicPartitions = new[]
                    {
                        new TopicPartitionOffset("topic1", Partition.Any, Offset.Beginning)
                    }.AsValueReadOnlyCollection()
                }
            }.AsValueReadOnlyCollection(),
        };

        KafkaConsumerEndpointsCache cache = new(configuration);

        KafkaConsumerEndpoint result = cache.GetEndpoint(new TopicPartition("topic1", 42));

        result.Configuration.ShouldBe(configuration.Endpoints.First());
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

        KafkaConsumerEndpoint result = cache.GetEndpoint(new TopicPartition("topic2", 42));

        result.TopicPartition.ShouldBe(new TopicPartition("topic2", Partition.Any));
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

        KafkaConsumerEndpoint result = cache.GetEndpoint(new TopicPartition("topic1", 42));

        result.TopicPartition.ShouldBe(new TopicPartition("topic1", 42));
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

        Exception exception = act.ShouldThrow<InvalidOperationException>();
        exception.Message.ShouldBe("No configuration found for the specified topic partition 'topic3[42]'.");
    }
}
