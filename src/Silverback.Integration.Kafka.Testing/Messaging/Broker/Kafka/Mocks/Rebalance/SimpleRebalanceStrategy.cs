// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using Confluent.Kafka;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Kafka.Mocks.Rebalance;

internal class SimpleRebalanceStrategy : IRebalanceStrategy
{
    public RebalanceResult Rebalance(
        IReadOnlyList<TopicPartition> partitionsToAssign,
        IReadOnlyList<SubscriptionPartitionAssignment> partitionAssignments)
    {
        Dictionary<IMockedConfluentConsumer, IReadOnlyCollection<TopicPartition>> revokedPartitions = new();

        foreach (SubscriptionPartitionAssignment assignment in partitionAssignments)
        {
            if (assignment.Partitions.Count > 0)
                revokedPartitions[assignment.Consumer] = assignment.Partitions.ToList(); // Clone via ToList

            assignment.Partitions.Clear();
        }

        IEnumerable<IGrouping<string, TopicPartition>> partitionsByTopic =
            partitionsToAssign.GroupBy(topicPartition => topicPartition.Topic);

        foreach (IGrouping<string, TopicPartition> topicPartitions in partitionsByTopic)
        {
            List<SubscriptionPartitionAssignment> partitionAssignmentsForTopic = partitionAssignments
                .Where(assignment => assignment.Consumer.Subscription.Contains(topicPartitions.Key)).ToList();

            AssignTopicPartitions(topicPartitions, partitionAssignmentsForTopic);
        }

        Dictionary<IMockedConfluentConsumer, IReadOnlyCollection<TopicPartition>> assignedPartitions =
            partitionAssignments
                .Where(assignment => assignment.Partitions.Count > 0)
                .ToDictionary(
                    assignment => assignment.Consumer,
                    assignment => assignment.Partitions.AsReadOnlyCollection());

        return new RebalanceResult(assignedPartitions, revokedPartitions);
    }

    private static void AssignTopicPartitions(
        IEnumerable<TopicPartition> topicPartitions,
        IReadOnlyList<PartitionAssignment> partitionAssignments)
    {
        int index = -1;

        foreach (TopicPartition topicPartition in topicPartitions)
        {
            if (++index >= partitionAssignments.Count)
                index = 0;

            partitionAssignments[index].Partitions.Add(topicPartition);
        }
    }
}
