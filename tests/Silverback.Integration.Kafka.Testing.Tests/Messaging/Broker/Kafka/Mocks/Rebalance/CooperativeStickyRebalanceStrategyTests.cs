// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Confluent.Kafka;
using FluentAssertions;
using NSubstitute;
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
            new TopicPartition("topic1", 0),
            new TopicPartition("topic1", 1),
            new TopicPartition("topic1", 2),
            new TopicPartition("topic1", 3),
            new TopicPartition("topic1", 4),
            new TopicPartition("topic2", 0),
            new TopicPartition("topic2", 1),
            new TopicPartition("topic2", 2)
        ];

        IMockedConfluentConsumer consumer1 = GetMockedConsumer("topic1", "topic2");
        IMockedConfluentConsumer consumer2 = GetMockedConsumer("topic1", "topic2");

        List<SubscriptionPartitionAssignment> partitionAssignments =
        [
            new SubscriptionPartitionAssignment(consumer1),
            new SubscriptionPartitionAssignment(consumer2)
        ];

        RebalanceResult result = new CooperativeStickyRebalanceStrategy().Rebalance(partitions, partitionAssignments);

        result.RevokedPartitions.Should().BeEmpty();

        result.AssignedPartitions.Should().HaveCount(2);
        result.AssignedPartitions[consumer1].Should().BeEquivalentTo(partitionAssignments[0].Partitions);
        result.AssignedPartitions[consumer2].Should().BeEquivalentTo(partitionAssignments[1].Partitions);

        partitionAssignments[0].Partitions.Should().BeEquivalentTo(
            new[]
            {
                new TopicPartition("topic1", 0),
                new TopicPartition("topic1", 1),
                new TopicPartition("topic1", 4),
                new TopicPartition("topic2", 0),
                new TopicPartition("topic2", 2)
            });
        partitionAssignments[1].Partitions.Should().BeEquivalentTo(
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
            new TopicPartition("topic1", 0),
            new TopicPartition("topic1", 1),
            new TopicPartition("topic1", 2),
            new TopicPartition("topic1", 3),
            new TopicPartition("topic1", 4),
            new TopicPartition("topic2", 0),
            new TopicPartition("topic2", 1),
            new TopicPartition("topic2", 2)
        ];

        IMockedConfluentConsumer consumer1 = GetMockedConsumer("topic1", "topic2");
        IMockedConfluentConsumer consumer2 = GetMockedConsumer("topic1", "topic2");
        IMockedConfluentConsumer consumer3 = GetMockedConsumer("topic1");

        List<SubscriptionPartitionAssignment> partitionAssignments =
        [
            new SubscriptionPartitionAssignment(consumer1),
            new SubscriptionPartitionAssignment(consumer2),
            new SubscriptionPartitionAssignment(consumer3)
        ];

        RebalanceResult result = new CooperativeStickyRebalanceStrategy().Rebalance(partitions, partitionAssignments);

        result.RevokedPartitions.Should().BeEmpty();

        result.AssignedPartitions.Should().HaveCount(3);
        result.AssignedPartitions[consumer1].Should().BeEquivalentTo(partitionAssignments[0].Partitions);
        result.AssignedPartitions[consumer2].Should().BeEquivalentTo(partitionAssignments[1].Partitions);
        result.AssignedPartitions[consumer3].Should().BeEquivalentTo(partitionAssignments[2].Partitions);

        partitionAssignments[0].Partitions.Should().BeEquivalentTo(
            new[]
            {
                new TopicPartition("topic1", 0),
                new TopicPartition("topic1", 3),
                new TopicPartition("topic2", 0),
                new TopicPartition("topic2", 2)
            });
        partitionAssignments[1].Partitions.Should().BeEquivalentTo(
            new[]
            {
                new TopicPartition("topic1", 1),
                new TopicPartition("topic1", 4),
                new TopicPartition("topic2", 1)
            });
        partitionAssignments[2].Partitions.Should().BeEquivalentTo(
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
            new TopicPartition("topic1", 0),
            new TopicPartition("topic1", 1),
            new TopicPartition("topic1", 2),
            new TopicPartition("topic1", 3),
            new TopicPartition("topic1", 4),
            new TopicPartition("topic1", 5),
            new TopicPartition("topic1", 6),
            new TopicPartition("topic1", 7),
            new TopicPartition("topic1", 8),
            new TopicPartition("topic1", 9)
        ];

        IMockedConfluentConsumer consumer1 = GetMockedConsumer("topic1");
        IMockedConfluentConsumer consumer2 = GetMockedConsumer("topic1");
        IMockedConfluentConsumer consumer3 = GetMockedConsumer("topic1");

        List<SubscriptionPartitionAssignment> partitionAssignments =
        [
            new SubscriptionPartitionAssignment(consumer1),
            new SubscriptionPartitionAssignment(consumer2),
            new SubscriptionPartitionAssignment(consumer3)
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

        result.RevokedPartitions.Should().HaveCount(2);
        result.RevokedPartitions[consumer1].Should().BeEquivalentTo(
            new[]
            {
                new TopicPartition("topic1", 3),
                new TopicPartition("topic1", 4)
            });
        result.RevokedPartitions[consumer2].Should().BeEquivalentTo(
            new[]
            {
                new TopicPartition("topic1", 9)
            });

        result.AssignedPartitions.Should().HaveCount(1);
        result.AssignedPartitions[consumer3].Should().BeEquivalentTo(partitionAssignments[2].Partitions);

        partitionAssignments[2].Partitions.Should().BeEquivalentTo(
            new[]
            {
                new TopicPartition("topic1", 3),
                new TopicPartition("topic1", 4),
                new TopicPartition("topic1", 9)
            });
    }

    [Fact]
    public void Rebalance_RemovingOneConsumer_PartitionsReassigned()
    {
        List<TopicPartition> partitions =
        [
            new TopicPartition("topic1", 0),
            new TopicPartition("topic1", 1),
            new TopicPartition("topic1", 2),
            new TopicPartition("topic1", 3),
            new TopicPartition("topic1", 4)
        ];

        IMockedConfluentConsumer consumer1 = GetMockedConsumer("topic1");
        IMockedConfluentConsumer consumer2 = GetMockedConsumer("topic1");

        List<SubscriptionPartitionAssignment> partitionAssignments =
        [
            new SubscriptionPartitionAssignment(consumer1),
            new SubscriptionPartitionAssignment(consumer2)
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

        result.RevokedPartitions.Should().BeEmpty();

        result.AssignedPartitions.Should().HaveCount(1);
        result.AssignedPartitions[consumer1].Should().BeEquivalentTo(
            new[]
            {
                new TopicPartition("topic1", 4)
            });

        partitionAssignments[0].Partitions.Should().BeEquivalentTo(
            new[]
            {
                new TopicPartition("topic1", 0),
                new TopicPartition("topic1", 1),
                new TopicPartition("topic1", 4)
            });
        partitionAssignments[1].Partitions.Should().BeEquivalentTo(
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
