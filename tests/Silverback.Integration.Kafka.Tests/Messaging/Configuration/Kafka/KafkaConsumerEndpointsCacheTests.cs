// Copyright (c) 2026 Sergio Aquilini
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

public class KafkaConsumerEndpointsCacheTests
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
            }.AsValueReadOnlyCollection()
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

    [Fact]
    public void GetEndpoint_ShouldReturnEndpointConfiguredForMatchingPattern()
    {
        KafkaConsumerConfiguration configuration = new()
        {
            Endpoints = new[]
            {
                new KafkaConsumerEndpointConfiguration
                {
                    TopicPartitions = new[]
                    {
                        new TopicPartitionOffset("topic1", Partition.Any, Offset.Unset)
                    }.AsValueReadOnlyCollection()
                },
                new KafkaConsumerEndpointConfiguration
                {
                    TopicPartitions = new[]
                    {
                        new TopicPartitionOffset("^pattern-[a-z]+$", Partition.Any, Offset.Unset)
                    }.AsValueReadOnlyCollection()
                }
            }.AsValueReadOnlyCollection()
        };

        KafkaConsumerEndpointsCache cache = new(configuration);

        KafkaConsumerEndpoint result = cache.GetEndpoint(new TopicPartition("pattern-abc", 1));

        result.Configuration.ShouldBe(configuration.Endpoints.Last());
        result.TopicPartition.ShouldBe(new TopicPartition("pattern-abc", Partition.Any));
    }

    [Fact]
    public void GetEndpoint_ShouldReturnSameEndpointForAllPartitionsOfTopicMatchingPattern()
    {
        KafkaConsumerConfiguration configuration = new()
        {
            Endpoints = new[]
            {
                new KafkaConsumerEndpointConfiguration
                {
                    TopicPartitions = new[]
                    {
                        new TopicPartitionOffset("^pattern-", Partition.Any, Offset.Unset)
                    }.AsValueReadOnlyCollection()
                }
            }.AsValueReadOnlyCollection()
        };

        KafkaConsumerEndpointsCache cache = new(configuration);

        KafkaConsumerEndpoint result1 = cache.GetEndpoint(new TopicPartition("pattern-abc", 0));
        KafkaConsumerEndpoint result2 = cache.GetEndpoint(new TopicPartition("pattern-abc", 1));
        KafkaConsumerEndpoint result3 = cache.GetEndpoint(new TopicPartition("pattern-abc", 0));
        KafkaConsumerEndpoint otherTopicResult = cache.GetEndpoint(new TopicPartition("pattern-def", 0));

        result2.ShouldBeSameAs(result1);
        result3.ShouldBeSameAs(result1);
        otherTopicResult.ShouldNotBeSameAs(result1);
        otherTopicResult.TopicPartition.ShouldBe(new TopicPartition("pattern-def", Partition.Any));
    }

    [Fact]
    public void GetEndpoint_ShouldPreferTopicNameOverPattern()
    {
        KafkaConsumerConfiguration configuration = new()
        {
            Endpoints = new[]
            {
                new KafkaConsumerEndpointConfiguration
                {
                    TopicPartitions = new[]
                    {
                        new TopicPartitionOffset("^topic", Partition.Any, Offset.Unset)
                    }.AsValueReadOnlyCollection()
                },
                new KafkaConsumerEndpointConfiguration
                {
                    TopicPartitions = new[]
                    {
                        new TopicPartitionOffset("topic2", Partition.Any, Offset.Unset)
                    }.AsValueReadOnlyCollection()
                }
            }.AsValueReadOnlyCollection()
        };

        KafkaConsumerEndpointsCache cache = new(configuration);

        KafkaConsumerEndpoint topic1Result = cache.GetEndpoint(new TopicPartition("topic1", 0));
        KafkaConsumerEndpoint topic2Result = cache.GetEndpoint(new TopicPartition("topic2", 0));

        topic1Result.Configuration.ShouldBe(configuration.Endpoints.First());
        topic2Result.Configuration.ShouldBe(configuration.Endpoints.Last());
    }

    [Fact]
    public void GetEndpoint_ShouldReturnEndpointConfiguredForFirstMatchingPattern()
    {
        KafkaConsumerConfiguration configuration = new()
        {
            Endpoints = new[]
            {
                new KafkaConsumerEndpointConfiguration
                {
                    TopicPartitions = new[]
                    {
                        new TopicPartitionOffset("topic1", Partition.Any, Offset.Unset),
                        new TopicPartitionOffset("^topic-a", Partition.Any, Offset.Unset)
                    }.AsValueReadOnlyCollection()
                },
                new KafkaConsumerEndpointConfiguration
                {
                    TopicPartitions = new[]
                    {
                        new TopicPartitionOffset("^topic-", Partition.Any, Offset.Unset)
                    }.AsValueReadOnlyCollection()
                }
            }.AsValueReadOnlyCollection()
        };

        KafkaConsumerEndpointsCache cache = new(configuration);

        KafkaConsumerEndpoint firstPatternResult = cache.GetEndpoint(new TopicPartition("topic-abc", 0));
        KafkaConsumerEndpoint secondPatternResult = cache.GetEndpoint(new TopicPartition("topic-def", 0));

        firstPatternResult.Configuration.ShouldBe(configuration.Endpoints.First());
        secondPatternResult.Configuration.ShouldBe(configuration.Endpoints.Last());
    }

    [Theory]
    [InlineData(null, 0)]
    [InlineData(GroupProtocol.Classic, 0)]
    [InlineData(GroupProtocol.Consumer, 1)]
    public void GetEndpoint_ShouldMatchPatternLikeBroker_WhenUsingConsumerProtocol(GroupProtocol? groupProtocol, int expectedEndpointIndex)
    {
        KafkaConsumerConfiguration configuration = new()
        {
            GroupProtocol = groupProtocol,
            Endpoints = new[]
            {
                new KafkaConsumerEndpointConfiguration
                {
                    TopicPartitions = new[]
                    {
                        new TopicPartitionOffset("^orders", Partition.Any, Offset.Unset)
                    }.AsValueReadOnlyCollection()
                },
                new KafkaConsumerEndpointConfiguration
                {
                    TopicPartitions = new[]
                    {
                        new TopicPartitionOffset("^orders-eu-.*", Partition.Any, Offset.Unset)
                    }.AsValueReadOnlyCollection()
                }
            }.AsValueReadOnlyCollection()
        };

        KafkaConsumerEndpointsCache cache = new(configuration);

        KafkaConsumerEndpoint result = cache.GetEndpoint(new TopicPartition("orders-eu-1", 0));

        result.Configuration.ShouldBe(configuration.Endpoints.ElementAt(expectedEndpointIndex));
    }

    [Fact]
    public void GetEndpoint_ShouldThrow_WhenNoPatternMatches()
    {
        KafkaConsumerConfiguration configuration = new()
        {
            Endpoints = new[]
            {
                new KafkaConsumerEndpointConfiguration
                {
                    TopicPartitions = new[]
                    {
                        new TopicPartitionOffset("topic1", Partition.Any, Offset.Unset),
                        new TopicPartitionOffset("^topic-[a-z]+$", Partition.Any, Offset.Unset),
                        new TopicPartitionOffset("^other-", Partition.Any, Offset.Unset)
                    }.AsValueReadOnlyCollection()
                }
            }.AsValueReadOnlyCollection()
        };

        KafkaConsumerEndpointsCache cache = new(configuration);

        Action act = () => cache.GetEndpoint(new TopicPartition("topic-42", 1));

        Exception exception = act.ShouldThrow<InvalidOperationException>();
        exception.Message.ShouldBe(
            "No configuration found for the specified topic partition 'topic-42[1]'. " +
            "The topic patterns '^topic-[a-z]+$', '^other-' don't match it when evaluated as .NET regular expressions.");
    }
}
