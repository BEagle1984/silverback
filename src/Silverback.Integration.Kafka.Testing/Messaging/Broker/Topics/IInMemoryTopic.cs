// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;

namespace Silverback.Messaging.Broker.Topics
{
    /// <summary>
    ///     Represent a mocked topic where the messages are just stored in memory. Note that it isn't obviously
    ///     possible to accurately replicate the message broker behavior and this implementation is just intended
    ///     for testing purposes.
    /// </summary>
    public interface IInMemoryTopic
    {
        /// <summary>
        ///     Gets the topic name.
        /// </summary>
        string Name { get; }

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
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        Offset Push(int partition, Message<byte[]?, byte[]?> message);

        /// <summary>
        ///     Pulls the next message from the topic, if available.
        /// </summary>
        /// <param name="groupId">
        ///     The consumer group id.
        /// </param>
        /// <param name="partitionOffsets">
        ///     The offset of the next message in each partition.
        /// </param>
        /// <param name="result">
        /// The <see cref="ConsumeResult{TKey,TValue}" /> wrapping the pulled message.
        /// </param>
        /// <returns>
        ///     A value indicating whether a message was available for pulling.
        /// </returns>
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        bool TryPull(
            string groupId,
            IReadOnlyCollection<TopicPartitionOffset> partitionOffsets,
            out ConsumeResult<byte[]?, byte[]?>? result);

        /// <summary>
        ///     Subscribes the consumer to the topic.
        /// </summary>
        /// <param name="consumer">
        ///     The <see cref="MockedKafkaConsumer" /> instance.
        /// </param>
        void Subscribe(MockedKafkaConsumer consumer);

        /// <summary>
        ///     Unsubscribes the consumer from the topic.
        /// </summary>
        /// <param name="consumer">
        ///     The <see cref="MockedKafkaConsumer" /> instance.
        /// </param>
        void Unsubscribe(MockedKafkaConsumer consumer);

        /// <summary>
        ///     Commits the offsets of the specified consumer group.
        /// </summary>
        /// <param name="groupId">
        ///     The consumer group id.
        /// </param>
        /// <param name="partitionOffsets">
        ///     The offsets to be committed.
        /// </param>
        void Commit(string groupId, IEnumerable<TopicPartitionOffset> partitionOffsets);

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
        ///     Gets the <see cref="Offset" /> of the latest message written to the specified partition.
        /// </summary>
        /// <param name="partition">
        ///     The partition.
        /// </param>
        /// <returns>
        ///     The <see cref="Offset" /> of the latest message in the partition.
        /// </returns>
        Offset GetLatestOffset(Partition partition);

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
    }
}
