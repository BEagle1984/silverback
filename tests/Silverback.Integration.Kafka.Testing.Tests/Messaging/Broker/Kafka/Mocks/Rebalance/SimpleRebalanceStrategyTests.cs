// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using Confluent.Kafka;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Broker.Kafka.Mocks;
using Silverback.Messaging.Broker.Kafka.Mocks.Rebalance;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Testing.Messaging.Broker.Kafka.Mocks.Rebalance
{
    public class SimpleRebalanceStrategyTests
    {
        [Fact]
        public void Rebalance_FirstRebalance_PartitionsAssigned()
        {
            var partitions = new List<TopicPartition>
            {
                new("topic1", 0),
                new("topic1", 1),
                new("topic1", 2),
                new("topic1", 3),
                new("topic1", 4),
                new("topic2", 0),
                new("topic2", 1),
                new("topic2", 2)
            };

            var consumer1 = GetMockedConsumer("topic1", "topic2");
            var consumer2 = GetMockedConsumer("topic1", "topic2");

            var partitionAssignments = new List<SubscriptionPartitionAssignment>
            {
                new(consumer1),
                new(consumer2)
            };

            var result = new SimpleRebalanceStrategy().Rebalance(partitions, partitionAssignments);

            result.RevokedPartitions.Should().BeEmpty();

            result.AssignedPartitions.Should().HaveCount(2);
            result.AssignedPartitions[consumer1].Should().BeEquivalentTo(partitionAssignments[0].Partitions);
            result.AssignedPartitions[consumer2].Should().BeEquivalentTo(partitionAssignments[1].Partitions);

            partitionAssignments[0].Partitions.Should().BeEquivalentTo(
                new[]
                {
                    new TopicPartition("topic1", 0),
                    new TopicPartition("topic1", 2),
                    new TopicPartition("topic1", 4),
                    new TopicPartition("topic2", 0),
                    new TopicPartition("topic2", 2)
                });
            partitionAssignments[1].Partitions.Should().BeEquivalentTo(
                new[]
                {
                    new TopicPartition("topic1", 1),
                    new TopicPartition("topic1", 3),
                    new TopicPartition("topic2", 1)
                });
        }

        [Fact]
        public void Rebalance_FirstRebalanceWithMixedSubscriptions_PartitionsAssigned()
        {
            var partitions = new List<TopicPartition>
            {
                new("topic1", 0),
                new("topic1", 1),
                new("topic1", 2),
                new("topic1", 3),
                new("topic1", 4),
                new("topic2", 0),
                new("topic2", 1),
                new("topic2", 2)
            };

            var consumer1 = GetMockedConsumer("topic1", "topic2");
            var consumer2 = GetMockedConsumer("topic1", "topic2");
            var consumer3 = GetMockedConsumer("topic1");

            var partitionAssignments = new List<SubscriptionPartitionAssignment>
            {
                new(consumer1),
                new(consumer2),
                new(consumer3)
            };

            var result = new SimpleRebalanceStrategy().Rebalance(partitions, partitionAssignments);

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
            var partitions = new List<TopicPartition>
            {
                new("topic1", 0),
                new("topic1", 1),
                new("topic1", 2),
                new("topic1", 3),
                new("topic1", 4)
            };

            var consumer1 = GetMockedConsumer("topic1");
            var consumer2 = GetMockedConsumer("topic1");
            var consumer3 = GetMockedConsumer("topic1");

            var partitionAssignments = new List<SubscriptionPartitionAssignment>
            {
                new(consumer1),
                new(consumer2),
                new(consumer3)
            };

            partitionAssignments[0].Partitions.AddRange(
                new List<TopicPartition>
                {
                    new("topic1", 0),
                    new("topic1", 2),
                    new("topic1", 4)
                });
            partitionAssignments[1].Partitions.AddRange(
                new List<TopicPartition>
                {
                    new("topic1", 1),
                    new("topic1", 3)
                });

            var result = new SimpleRebalanceStrategy().Rebalance(partitions, partitionAssignments);

            result.RevokedPartitions.Should().HaveCount(2);
            result.RevokedPartitions[consumer1].Should().BeEquivalentTo(
                new[]
                {
                    new TopicPartition("topic1", 0),
                    new TopicPartition("topic1", 2),
                    new TopicPartition("topic1", 4)
                });
            result.RevokedPartitions[consumer2].Should().BeEquivalentTo(
                new[]
                {
                    new TopicPartition("topic1", 1),
                    new TopicPartition("topic1", 3)
                });

            result.AssignedPartitions.Should().HaveCount(3);
            result.AssignedPartitions[consumer1].Should().BeEquivalentTo(partitionAssignments[0].Partitions);
            result.AssignedPartitions[consumer2].Should().BeEquivalentTo(partitionAssignments[1].Partitions);
            result.AssignedPartitions[consumer3].Should().BeEquivalentTo(partitionAssignments[2].Partitions);

            partitionAssignments[0].Partitions.Should().BeEquivalentTo(
                new[]
                {
                    new TopicPartition("topic1", 0),
                    new TopicPartition("topic1", 3)
                });
            partitionAssignments[1].Partitions.Should().BeEquivalentTo(
                new[]
                {
                    new TopicPartition("topic1", 1),
                    new TopicPartition("topic1", 4)
                });
            partitionAssignments[2].Partitions.Should().BeEquivalentTo(
                new[]
                {
                    new TopicPartition("topic1", 2)
                });
        }

        [Fact]
        public void Rebalance_RemovingOneConsumer_PartitionsReassigned()
        {
            var partitions = new List<TopicPartition>
            {
                new("topic1", 0),
                new("topic1", 1),
                new("topic1", 2),
                new("topic1", 3),
                new("topic1", 4)
            };

            var consumer1 = GetMockedConsumer("topic1");
            var consumer2 = GetMockedConsumer("topic1");

            var partitionAssignments = new List<SubscriptionPartitionAssignment>
            {
                new(consumer1),
                new(consumer2)
            };

            partitionAssignments[0].Partitions.AddRange(
                new List<TopicPartition>
                {
                    new("topic1", 0),
                    new("topic1", 3)
                });
            partitionAssignments[1].Partitions.AddRange(
                new List<TopicPartition>
                {
                    new("topic1", 1),
                    new("topic1", 4)
                });

            var result = new SimpleRebalanceStrategy().Rebalance(partitions, partitionAssignments);

            result.RevokedPartitions.Should().HaveCount(2);
            result.RevokedPartitions[consumer1].Should().BeEquivalentTo(
                new[]
                {
                    new TopicPartition("topic1", 0),
                    new TopicPartition("topic1", 3)
                });
            result.RevokedPartitions[consumer2].Should().BeEquivalentTo(
                new[]
                {
                    new TopicPartition("topic1", 1),
                    new TopicPartition("topic1", 4)
                });

            result.AssignedPartitions.Should().HaveCount(2);
            result.AssignedPartitions[consumer1].Should().BeEquivalentTo(partitionAssignments[0].Partitions);
            result.AssignedPartitions[consumer2].Should().BeEquivalentTo(partitionAssignments[1].Partitions);

            partitionAssignments[0].Partitions.Should().BeEquivalentTo(
                new[]
                {
                    new TopicPartition("topic1", 0),
                    new TopicPartition("topic1", 2),
                    new TopicPartition("topic1", 4)
                });
            partitionAssignments[1].Partitions.Should().BeEquivalentTo(
                new[]
                {
                    new TopicPartition("topic1", 1),
                    new TopicPartition("topic1", 3)
                });
        }

        private static IMockedConfluentConsumer GetMockedConsumer(params string[] topics)
        {
            var consumer = Substitute.For<IMockedConfluentConsumer>();
            consumer.Subscription.Returns(topics.ToList());
            return consumer;
        }
    }
}
