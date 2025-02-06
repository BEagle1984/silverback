// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;

namespace Silverback.Messaging.Broker.Kafka.Mocks;

/// <summary>
///     A mocked consumer group. Note that it isn't obviously possible to accurately replicate the message broker
///     behavior and this implementation is just intended for testing purposes.
/// </summary>
public interface IMockedConsumerGroup
{
    /// <summary>
    ///     Gets the consumer group id.
    /// </summary>
    string GroupId { get; }

    /// <summary>
    ///     Gets the bootstrap servers string used to identify the target broker.
    /// </summary>
    string BootstrapServers { get; }

    /// <summary>
    ///     Gets a value indicating whether a rebalance is being performed.
    /// </summary>
    bool IsRebalancing { get; }

    /// <summary>
    ///     Gets a value indicating whether a rebalance has been scheduled.
    /// </summary>
    bool IsRebalanceScheduled { get; }

    /// <summary>
    ///     Gets the latest committed <see cref="Offset" /> for each topic partition.
    /// </summary>
    /// <returns>
    ///     The collection containing the latest <see cref="Offset" /> for each topic partition.
    /// </returns>
    IReadOnlyCollection<TopicPartitionOffset> CommittedOffsets { get; }

    /// <summary>
    ///     Subscribes the consumer to the specified topics.
    /// </summary>
    /// <param name="consumer">
    ///     The <see cref="IMockedConfluentConsumer" /> instance.
    /// </param>
    /// <param name="topics">
    ///     The topics to be subscribed.
    /// </param>
    void Subscribe(IMockedConfluentConsumer consumer, IEnumerable<string> topics);

    /// <summary>
    ///     Unsubscribes the consumer from all topics.
    /// </summary>
    /// <param name="consumer">
    ///     The <see cref="IMockedConfluentConsumer" /> instance.
    /// </param>
    void Unsubscribe(IMockedConfluentConsumer consumer);

    /// <summary>
    ///     Assigns the specified partitions to the consumer.
    /// </summary>
    /// <param name="consumer">
    ///     The <see cref="IMockedConfluentConsumer" /> instance.
    /// </param>
    /// <param name="partitions">
    ///     The partitions.
    /// </param>
    void Assign(IMockedConfluentConsumer consumer, IEnumerable<TopicPartition> partitions);

    /// <summary>
    ///     Removes the partitions assignment for the specified consumer.
    /// </summary>
    /// <param name="consumer">
    ///     The <see cref="IMockedConfluentConsumer" /> instance.
    /// </param>
    void Unassign(IMockedConfluentConsumer consumer);

    /// <summary>
    ///     Removes the consumer from the group.
    /// </summary>
    /// <remarks>
    ///     This method is called when the <see cref="IMockedConfluentConsumer" /> is closed or disposed.
    /// </remarks>
    /// <param name="consumer">
    ///     The <see cref="IMockedConfluentConsumer" /> instance.
    /// </param>
    void Remove(IMockedConfluentConsumer consumer);

    /// <summary>
    ///     Commits the specified offsets.
    /// </summary>
    /// <param name="offsets">
    ///     The offsets to be committed.
    /// </param>
    void Commit(IEnumerable<TopicPartitionOffset> offsets);

    /// <summary>
    ///     Initializes a rebalance and causes all assignments to be revoked and reassigned.
    /// </summary>
    void ScheduleRebalance();

    /// <summary>
    ///     Exectues a rebalance and causes all assignments to be revoked and reassigned.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains the
    ///     partitions that have been revoked and assigned.
    /// </returns>
    Task<RebalanceResult> RebalanceAsync();

    /// <summary>
    ///     Gets partition assignment for the specified consumer.
    /// </summary>
    /// <param name="consumer">
    ///     The consumer.
    /// </param>
    /// <returns>
    ///     The partitions currently assigned to the consumer.
    /// </returns>
    IReadOnlyCollection<TopicPartition> GetAssignment(IMockedConfluentConsumer consumer);

    /// <summary>
    ///     Gets the latest committed <see cref="Offset" /> for the specified topic partition.
    /// </summary>
    /// <param name="topicPartition">
    ///     The topic partition.
    /// </param>
    /// <returns>
    ///     The latest committed <see cref="Offset" /> for the topic partition, or <c>null</c> if no offset has been committed
    ///     for this partition.
    /// </returns>
    TopicPartitionOffset? GetCommittedOffset(TopicPartition topicPartition);

    /// <summary>
    ///     Gets the total number of committed offsets. This number is usually equal to the number of consumed messages.
    /// </summary>
    /// <param name="topic">
    ///     The topic.
    /// </param>
    /// <returns>
    ///     The number of committed offsets.
    /// </returns>
    long GetCommittedOffsetsCount(string topic);

    /// <summary>
    ///     Returns a <see cref="Task" /> that completes when all messages routed to the consumers have been processed and committed.
    /// </summary>
    /// <param name="topicNames">
    ///     The names of the topics to wait for. If not specified, all topics are considered.
    /// </param>
    /// <param name="cancellationToken">
    ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> that completes when all messages have been processed.
    /// </returns>
    ValueTask WaitUntilAllMessagesAreConsumedAsync(IReadOnlyCollection<string> topicNames, CancellationToken cancellationToken = default);
}
