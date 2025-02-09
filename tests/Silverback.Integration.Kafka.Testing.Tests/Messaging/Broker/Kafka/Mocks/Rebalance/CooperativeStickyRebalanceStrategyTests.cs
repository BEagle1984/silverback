// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Confluent.Kafka;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker.Kafka.Mocks;
using Silverback.Messaging.Broker.Kafka.Mocks.Rebalance;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Testing.Messaging.Broker.Kafka.Mocks.Rebalance;

public class CooperativeStickyRebalanceStrategyTests
{
    [Fact]
    public void Rebalance_FirstRebalance_PartitionsAssigned()
    {
        List<TopicPartition> partitions =
        [
            new("topic1", 0),
            new("topic1", 1),
            new("topic1", 2),
            new("topic1", 3),
            new("topic1", 4),
            new("topic2", 0),
            new("topic2", 1),
            new("topic2", 2)
        ];

        IMockedConfluentConsumer consumer1 = GetMockedConsumer("topic1", "topic2");
        IMockedConfluentConsumer consumer2 = GetMockedConsumer("topic1", "topic2");

        List<SubscriptionPartitionAssignment> partitionAssignments =
        [
            new(consumer1),
            new(consumer2)
        ];

        RebalanceResult result = new CooperativeStickyRebalanceStrategy().Rebalance(partitions, partitionAssignments);

        result.RevokedPartitions.ShouldBeEmpty();

        result.AssignedPartitions.Count.ShouldBe(2);
        result.AssignedPartitions[consumer1].ShouldBe(partitionAssignments[0].Partitions);
        result.AssignedPartitions[consumer2].ShouldBe(partitionAssignments[1].Partitions);

        partitionAssignments[0].Partitions.ShouldBe(
            new[]
            {
                new TopicPartition("topic1", 0),
                new TopicPartition("topic1", 1),
                new TopicPartition("topic1", 4),
                new TopicPartition("topic2", 0),
                new TopicPartition("topic2", 2)
            });
        partitionAssignments[1].Partitions.ShouldBe(
            new[]
            {
                new TopicPartition("topic1", 2),
                new TopicPartition("topic1", 3),
                new TopicPartition("topic2", 1)
            });
    }

    [Fact]
    public void Rebalance_FirstRebalanceWithMixedSubscriptions_PartitionsAssigned()
    {
        List<TopicPartition> partitions =
        [
            new("topic1", 0),
            new("topic1", 1),
            new("topic1", 2),
            new("topic1", 3),
            new("topic1", 4),
            new("topic2", 0),
            new("topic2", 1),
            new("topic2", 2)
        ];

        IMockedConfluentConsumer consumer1 = GetMockedConsumer("topic1", "topic2");
        IMockedConfluentConsumer consumer2 = GetMockedConsumer("topic1", "topic2");
        IMockedConfluentConsumer consumer3 = GetMockedConsumer("topic1");

        List<SubscriptionPartitionAssignment> partitionAssignments =
        [
            new(consumer1),
            new(consumer2),
            new(consumer3)
        ];

        RebalanceResult result = new CooperativeStickyRebalanceStrategy().Rebalance(partitions, partitionAssignments);

        result.RevokedPartitions.ShouldBeEmpty();

        result.AssignedPartitions.Count.ShouldBe(3);
        result.AssignedPartitions[consumer1].ShouldBe(partitionAssignments[0].Partitions);
        result.AssignedPartitions[consumer2].ShouldBe(partitionAssignments[1].Partitions);
        result.AssignedPartitions[consumer3].ShouldBe(partitionAssignments[2].Partitions);

        partitionAssignments[0].Partitions.ShouldBe(
            new[]
            {
                new TopicPartition("topic1", 0),
                new TopicPartition("topic1", 3),
                new TopicPartition("topic2", 0),
                new TopicPartition("topic2", 2)
            });
        partitionAssignments[1].Partitions.ShouldBe(
            new[]
            {
                new TopicPartition("topic1", 1),
                new TopicPartition("topic1", 4),
                new TopicPartition("topic2", 1)
            });
        partitionAssignments[2].Partitions.ShouldBe(
            new[]
            {
                new TopicPartition("topic1", 2)
            });
    }

    [Fact]
    public void Rebalance_AddingOneConsumer_PartitionsReassigned()
    {
        List<TopicPartition> partitions =
        [
            new("topic1", 0),
            new("topic1", 1),
            new("topic1", 2),
            new("topic1", 3),
            new("topic1", 4),
            new("topic1", 5),
            new("topic1", 6),
            new("topic1", 7),
            new("topic1", 8),
            new("topic1", 9)
        ];

        IMockedConfluentConsumer consumer1 = GetMockedConsumer("topic1");
        IMockedConfluentConsumer consumer2 = GetMockedConsumer("topic1");
        IMockedConfluentConsumer consumer3 = GetMockedConsumer("topic1");

        List<SubscriptionPartitionAssignment> partitionAssignments =
        [
            new(consumer1),
            new(consumer2),
            new(consumer3)
        ];

        partitionAssignments[0].Partitions.AddRange(
        [
            new TopicPartition("topic1", 0),
            new TopicPartition("topic1", 1),
            new TopicPartition("topic1", 2),
            new TopicPartition("topic1", 3),
            new TopicPartition("topic1", 4)
        ]);
        partitionAssignments[1].Partitions.AddRange(
        [
            new TopicPartition("topic1", 5),
            new TopicPartition("topic1", 6),
            new TopicPartition("topic1", 7),
            new TopicPartition("topic1", 8),
            new TopicPartition("topic1", 9)
        ]);

        RebalanceResult result = new CooperativeStickyRebalanceStrategy().Rebalance(partitions, partitionAssignments);

        result.RevokedPartitions.Count.ShouldBe(2);
        result.RevokedPartitions[consumer1].ShouldBe(
            [
                new TopicPartition("topic1", 3),
                new TopicPartition("topic1", 4)
            ],
            ignoreOrder: true);
        result.RevokedPartitions[consumer2].ShouldBe(
        [
            new TopicPartition("topic1", 9)
        ]);

        result.AssignedPartitions.Count.ShouldBe(1);
        result.AssignedPartitions[consumer3].ShouldBe(partitionAssignments[2].Partitions);

        partitionAssignments[2].Partitions.ShouldBe(
            [
                new TopicPartition("topic1", 3),
                new TopicPartition("topic1", 4),
                new TopicPartition("topic1", 9)
            ],
            ignoreOrder: true);
    }

    [Fact]
    public void Rebalance_RemovingOneConsumer_PartitionsReassigned()
    {
        List<TopicPartition> partitions =
        [
            new("topic1", 0),
            new("topic1", 1),
            new("topic1", 2),
            new("topic1", 3),
            new("topic1", 4)
        ];

        IMockedConfluentConsumer consumer1 = GetMockedConsumer("topic1");
        IMockedConfluentConsumer consumer2 = GetMockedConsumer("topic1");

        List<SubscriptionPartitionAssignment> partitionAssignments =
        [
            new(consumer1),
            new(consumer2)
        ];

        partitionAssignments[0].Partitions.AddRange(
        [
            new TopicPartition("topic1", 0),
            new TopicPartition("topic1", 1)
        ]);
        partitionAssignments[1].Partitions.AddRange(
        [
            new TopicPartition("topic1", 2),
            new TopicPartition("topic1", 3)
        ]);

        RebalanceResult result = new CooperativeStickyRebalanceStrategy().Rebalance(partitions, partitionAssignments);

        result.RevokedPartitions.ShouldBeEmpty();

        result.AssignedPartitions.Count.ShouldBe(1);
        result.AssignedPartitions[consumer1].ShouldBe(
        [
            new TopicPartition("topic1", 4)
        ]);

        partitionAssignments[0].Partitions.ShouldBe(
            new[]
            {
                new TopicPartition("topic1", 0),
                new TopicPartition("topic1", 1),
                new TopicPartition("topic1", 4)
            });
        partitionAssignments[1].Partitions.ShouldBe(
            new[]
            {
                new TopicPartition("topic1", 2),
                new TopicPartition("topic1", 3)
            });
    }

    private static IMockedConfluentConsumer GetMockedConsumer(params string[] topics)
    {
        IMockedConfluentConsumer? consumer = Substitute.For<IMockedConfluentConsumer>();
        consumer.Subscription.Returns<List<string>>([.. topics]);
        return consumer;
    }
}
