// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using Confluent.Kafka;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Kafka.Mocks.Rebalance;

internal class CooperativeStickyRebalanceStrategy : IRebalanceStrategy
{
    public RebalanceResult Rebalance(
        IReadOnlyList<TopicPartition> partitionsToAssign,
        IReadOnlyList<SubscriptionPartitionAssignment> partitionAssignments)
    {
        Dictionary<IMockedConfluentConsumer, List<TopicPartition>> assignedPartitions = [];
        Dictionary<IMockedConfluentConsumer, List<TopicPartition>> revokedPartitions = [];

        IEnumerable<IGrouping<string, TopicPartition>> partitionsByTopic =
            partitionsToAssign.GroupBy(topicPartition => topicPartition.Topic);

        foreach (IGrouping<string, TopicPartition> topicPartitions in partitionsByTopic)
        {
            List<SubscriptionPartitionAssignment> partitionAssignmentsForTopic = partitionAssignments
                .Where(assignment => assignment.Consumer.Subscription.Contains(topicPartitions.Key)).ToList();

            AssignTopicPartitions(
                [.. topicPartitions],
                partitionAssignmentsForTopic,
                assignedPartitions,
                revokedPartitions);
        }

        return new RebalanceResult(
            assignedPartitions.ToDictionary(pair => pair.Key, pair => pair.Value.AsReadOnlyCollection()),
            revokedPartitions.ToDictionary(pair => pair.Key, pair => pair.Value.AsReadOnlyCollection()));
    }

    private static void AssignTopicPartitions(
        IReadOnlyCollection<TopicPartition> topicPartitions,
        IReadOnlyList<PartitionAssignment> partitionAssignments,
        Dictionary<IMockedConfluentConsumer, List<TopicPartition>> assignedPartitions,
        Dictionary<IMockedConfluentConsumer, List<TopicPartition>> revokedPartitions)
    {
        List<TopicPartition> unassignedPartitions = topicPartitions
            .Where(
                topicPartition =>
                    partitionAssignments.All(assignment => !assignment.Partitions.Contains(topicPartition)))
            .ToList();

        int partitionsPerConsumer = topicPartitions.Count / partitionAssignments.Count;

        foreach (PartitionAssignment assignment in partitionAssignments)
        {
            while (assignment.Partitions.Count < partitionsPerConsumer)
            {
                if (AssignUnassignedPartition(assignment, unassignedPartitions, assignedPartitions))
                    continue;

                if (MoveAssignedPartition(assignment, partitionAssignments, partitionsPerConsumer, assignedPartitions, revokedPartitions))
                    continue;

                break;
            }
        }

        AssignRemainingPartitions(unassignedPartitions, partitionAssignments, assignedPartitions);
    }

    private static bool AssignUnassignedPartition(
        PartitionAssignment assignment,
        List<TopicPartition> unassignedPartitions,
        Dictionary<IMockedConfluentConsumer, List<TopicPartition>> assignedPartitions)
    {
        if (unassignedPartitions.Count > 0)
        {
            TopicPartition topicPartition = unassignedPartitions.First();

            assignment.Partitions.Add(topicPartition);
            assignedPartitions.GetOrAddDefault(assignment.Consumer).Add(topicPartition);
            unassignedPartitions.Remove(topicPartition);

            return true;
        }

        return false;
    }

    private static bool MoveAssignedPartition(
        PartitionAssignment assignment,
        IReadOnlyList<PartitionAssignment> partitionAssignments,
        int partitionsPerConsumer,
        Dictionary<IMockedConfluentConsumer, List<TopicPartition>> assignedPartitions,
        Dictionary<IMockedConfluentConsumer, List<TopicPartition>> revokedPartitions)
    {
        PartitionAssignment? revokableAssignment = FindRevokablaAssignment(partitionAssignments, partitionsPerConsumer);
        if (revokableAssignment != null)
        {
            TopicPartition topicPartition = revokableAssignment.Partitions.Last();

            assignment.Partitions.Add(topicPartition);
            assignedPartitions.GetOrAddDefault(assignment.Consumer).Add(topicPartition);
            revokableAssignment.Partitions.Remove(topicPartition);
            revokedPartitions.GetOrAddDefault(revokableAssignment.Consumer).Add(topicPartition);

            return true;
        }

        return false;
    }

    private static PartitionAssignment? FindRevokablaAssignment(
        IReadOnlyList<PartitionAssignment> partitionAssignments,
        int partitionsPerConsumer) =>
        partitionAssignments.FirstOrDefault(assignment => assignment.Partitions.Count > partitionsPerConsumer);

    private static void AssignRemainingPartitions(
        IEnumerable<TopicPartition> topicPartitions,
        IReadOnlyList<PartitionAssignment> partitionAssignments,
        Dictionary<IMockedConfluentConsumer, List<TopicPartition>> assignedPartitions)
    {
        int index = -1;

        foreach (TopicPartition topicPartition in topicPartitions)
        {
            if (++index >= partitionAssignments.Count)
                index = 0;

            partitionAssignments[index].Partitions.Add(topicPartition);
            assignedPartitions.GetOrAddDefault(partitionAssignments[index].Consumer).Add(topicPartition);
        }
    }
}
