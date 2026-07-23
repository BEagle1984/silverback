// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Confluent.Kafka;

namespace Silverback.Messaging.Broker.Kafka.Mocks;

/// <summary>
///     Exposes the members of a mocked consumer group used internally by the mocked consumers.
/// </summary>
internal interface IInternalMockedConsumerGroup : IMockedConsumerGroup
{
    /// <summary>
    ///     Gets a value indicating whether the group is rebalancing.
    /// </summary>
    bool IsRebalancing { get; }

    /// <summary>
    ///     Gets a value indicating whether a rebalance is scheduled.
    /// </summary>
    bool IsRebalanceScheduled { get; }

    /// <summary>
    ///     Subscribes the consumer to the specified topics.
    /// </summary>
    /// <param name="consumer">The consumer.</param>
    /// <param name="topics">The topics.</param>
    void Subscribe(IMockedConfluentConsumer consumer, IEnumerable<string> topics);

    /// <summary>
    ///     Unsubscribes the consumer.
    /// </summary>
    /// <param name="consumer">The consumer.</param>
    void Unsubscribe(IMockedConfluentConsumer consumer);

    /// <summary>
    ///     Assigns the specified partitions to the consumer.
    /// </summary>
    /// <param name="consumer">The consumer.</param>
    /// <param name="partitions">The partitions.</param>
    void Assign(IMockedConfluentConsumer consumer, IEnumerable<TopicPartition> partitions);

    /// <summary>
    ///     Unassigns the consumer.
    /// </summary>
    /// <param name="consumer">The consumer.</param>
    void Unassign(IMockedConfluentConsumer consumer);

    /// <summary>
    ///     Removes the consumer from the group.
    /// </summary>
    /// <param name="consumer">The consumer.</param>
    void Remove(IMockedConfluentConsumer consumer);

    /// <summary>
    ///     Commits the specified offsets.
    /// </summary>
    /// <param name="offsets">The offsets.</param>
    void Commit(IEnumerable<TopicPartitionOffset> offsets);

    /// <summary>
    ///     Gets the current assignment for the consumer.
    /// </summary>
    /// <param name="consumer">The consumer.</param>
    /// <returns>The current partition assignment.</returns>
    IReadOnlyCollection<TopicPartition> GetAssignment(IMockedConfluentConsumer consumer);

    /// <summary>
    ///     Signals that the consumer completed the current assignment.
    /// </summary>
    /// <param name="consumer">The consumer.</param>
    void NotifyAssignmentComplete(MockedConfluentConsumer consumer);
}
