// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Confluent.Kafka;

namespace Silverback.Messaging.Broker.Kafka.Mocks;

/// <summary>
///     Contains the partitions that have been revoked and assigned during the rebalance operation.
/// </summary>
public class RebalanceResult
{
    /// <summary>
    ///     An empty <see cref="RebalanceResult" />.
    /// </summary>
    public static readonly RebalanceResult Empty = new(
        new Dictionary<IMockedConfluentConsumer, IReadOnlyCollection<TopicPartition>>(),
        new Dictionary<IMockedConfluentConsumer, IReadOnlyCollection<TopicPartition>>());

    /// <summary>
    ///     Initializes a new instance of the <see cref="RebalanceResult" /> class.
    /// </summary>
    /// <param name="assignedPartitions">
    ///     The partitions that have been assigned.
    /// </param>
    /// <param name="revokedPartitions">
    ///     The partitions that have been revoked.
    /// </param>
    public RebalanceResult(
        IReadOnlyDictionary<IMockedConfluentConsumer, IReadOnlyCollection<TopicPartition>> assignedPartitions,
        IReadOnlyDictionary<IMockedConfluentConsumer, IReadOnlyCollection<TopicPartition>> revokedPartitions)
    {
        AssignedPartitions = assignedPartitions;
        RevokedPartitions = revokedPartitions;
    }

    /// <summary>
    ///     Gets the partitions that have been assigned to each involved consumer.
    /// </summary>
    public IReadOnlyDictionary<IMockedConfluentConsumer, IReadOnlyCollection<TopicPartition>> AssignedPartitions { get; }

    /// <summary>
    ///     Gets the partitions that have been revoked from each involved consumer.
    /// </summary>
    public IReadOnlyDictionary<IMockedConfluentConsumer, IReadOnlyCollection<TopicPartition>> RevokedPartitions { get; }
}
