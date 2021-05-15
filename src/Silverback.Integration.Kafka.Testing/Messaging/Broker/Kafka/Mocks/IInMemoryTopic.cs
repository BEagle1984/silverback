// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;

namespace Silverback.Messaging.Broker.Kafka.Mocks
{
    /// <summary>
    ///     A mocked topic where the messages are just stored in memory. Note that it isn't obviously possible to
    ///     accurately replicate the message broker behavior and this implementation is just intended for testing
    ///     purposes.
    /// </summary>
    public interface IInMemoryTopic
    {
        /// <summary>
        ///     Gets the topic name.
        /// </summary>
        string Name { get; }

        /// <summary>
        ///     Gets the partitions in the topic.
        /// </summary>
        IReadOnlyList<IInMemoryPartition> Partitions { get; }

        /// <summary>
        ///     Gets the total number of messages written into all the partitions of the topic.
        /// </summary>
        int MessagesCount { get; }

        /// <summary>
        ///     Gets all messages written into all the partitions of the topic.
        /// </summary>
        /// <returns>
        ///     The messages written into the topic.
        /// </returns>
        IReadOnlyList<Message<byte[]?, byte[]?>> GetAllMessages();

        /// <summary>
        ///     Writes a message to the topic.
        /// </summary>
        /// <param name="partition">
        ///     The index of the partition to be written to.
        /// </param>
        /// <param name="message">
        ///     The message to be written.
        /// </param>
        /// <returns>
        ///     The <see cref="Offset" /> at which the message was written.
        /// </returns>
        Offset Push(int partition, Message<byte[]?, byte[]?> message);

        /// <summary>
        ///     Pulls the next message from the topic, if available.
        /// </summary>
        /// <param name="consumer">
        ///     The consumer.
        /// </param>
        /// <param name="partitionOffsets">
        ///     The offset of the next message in each partition.
        /// </param>
        /// <param name="result">
        ///     The <see cref="ConsumeResult{TKey,TValue}" /> wrapping the pulled message.
        /// </param>
        /// <returns>
        ///     A value indicating whether a message was available for pulling.
        /// </returns>
        bool TryPull(
            IMockedConfluentConsumer consumer,
            IReadOnlyCollection<TopicPartitionOffset> partitionOffsets,
            out ConsumeResult<byte[]?, byte[]?>? result);

        /// <summary>
        ///     Ensures that a partition assignment has been given to the specified consumer, otherwise
        ///     triggers the assignment process.
        /// </summary>
        /// <param name="consumer">
        ///     The consumer.
        /// </param>
        /// <param name="assignmentDelay">
        ///     The delay to be applied before assigning the partitions.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        void EnsurePartitionsAssigned(
            IMockedConfluentConsumer consumer,
            TimeSpan assignmentDelay,
            CancellationToken cancellationToken);

        /// <summary>
        ///     Subscribes the consumer to the topic.
        /// </summary>
        /// <param name="consumer">
        ///     The <see cref="IMockedConfluentConsumer" /> instance.
        /// </param>
        void Subscribe(IMockedConfluentConsumer consumer);

        /// <summary>
        ///     Unsubscribes the consumer from the topic.
        /// </summary>
        /// <param name="consumer">
        ///     The <see cref="IMockedConfluentConsumer" /> instance.
        /// </param>
        void Unsubscribe(IMockedConfluentConsumer consumer);

        /// <summary>
        ///     Assigns the specified partition to the consumer.
        /// </summary>
        /// <param name="consumer">
        ///     The <see cref="IMockedConfluentConsumer" /> instance.
        /// </param>
        /// <param name="partition">
        ///     The partition.
        /// </param>
        void Assign(IMockedConfluentConsumer consumer, Partition partition);

        /// <summary>
        ///     Commits the offsets of the specified consumer group.
        /// </summary>
        /// <param name="groupId">
        ///     The consumer group id.
        /// </param>
        /// <param name="partitionOffsets">
        ///     The offsets to be committed.
        /// </param>
        /// <returns>
        ///     The actual committed offsets.
        /// </returns>
        IReadOnlyCollection<TopicPartitionOffset> Commit(
            string groupId,
            IEnumerable<TopicPartitionOffset> partitionOffsets);

        /// <summary>
        ///     Gets the latest committed <see cref="Offset" /> for the specified partition.
        /// </summary>
        /// <param name="partition">
        ///     The partition.
        /// </param>
        /// <param name="groupId">
        ///     The consumer group id.
        /// </param>
        /// <returns>
        ///     The latest <see cref="Offset" />.
        /// </returns>
        Offset GetCommittedOffset(Partition partition, string groupId);

        /// <summary>
        ///     Gets the latest committed <see cref="Offset" /> for each partition.
        /// </summary>
        /// <param name="groupId">
        ///     The consumer group id.
        /// </param>
        /// <returns>
        ///     The collection containing the latest <see cref="Offset" /> for each partition.
        /// </returns>
        IReadOnlyCollection<TopicPartitionOffset> GetCommittedOffsets(string groupId);

        /// <summary>
        ///     Gets the number of committed offsets for the specified consumer group. This number is usually equal to
        ///     the number of consumed messages.
        /// </summary>
        /// <param name="groupId">
        ///     The consumer group id.
        /// </param>
        /// <returns>
        ///     The number of committed offsets.
        /// </returns>
        long GetCommittedOffsetsCount(string groupId);

        /// <summary>
        ///     Gets the <see cref="Offset" /> of the first message in the specified partition.
        /// </summary>
        /// <param name="partition">
        ///     The partition.
        /// </param>
        /// <returns>
        ///     The <see cref="Offset" /> of the first message in the partition.
        /// </returns>
        Offset GetFirstOffset(Partition partition);

        /// <summary>
        ///     Gets the <see cref="Offset" /> of the latest message written to the specified partition.
        /// </summary>
        /// <param name="partition">
        ///     The partition.
        /// </param>
        /// <returns>
        ///     The <see cref="Offset" /> of the latest message in the partition.
        /// </returns>
        Offset GetLastOffset(Partition partition);

        /// <summary>
        ///     Returns a <see cref="Task" /> that completes when all messages routed to the consumers have been
        ///     processed and committed.
        /// </summary>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> that completes when all messages have been processed.
        /// </returns>
        Task WaitUntilAllMessagesAreConsumedAsync(CancellationToken cancellationToken = default);

        /// <summary>
        ///     Simulates a rebalance and causes all assignments to be revoked and reassigned.
        /// </summary>
        void Rebalance();
    }
}
