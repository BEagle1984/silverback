// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Messaging.Broker.Kafka.Mocks;

/// <summary>
///     Exposes the state of a consumer group in the in-memory cluster.
/// </summary>
public interface IMockedConsumerGroup
{
    /// <summary>
    ///     Gets the consumer group id.
    /// </summary>
    string GroupId { get; }

    /// <summary>
    ///     Gets the offsets committed by the consumer group.
    /// </summary>
    IReadOnlyCollection<TopicPartitionOffset> CommittedOffsets { get; }

    /// <summary>
    ///     Gets the offset committed by the consumer group for the specified partition.
    /// </summary>
    /// <param name="topicPartition">
    ///     The topic partition.
    /// </param>
    /// <returns>
    ///     The committed offset, or <c>null</c> if no offset has been committed.
    /// </returns>
    TopicPartitionOffset? GetCommittedOffset(TopicPartition topicPartition);

    /// <summary>
    ///     Gets the sum of the offsets committed by the consumer group for all partitions of the specified topic. It
    ///     corresponds to the number of consumed messages when the topic contains no transaction markers.
    /// </summary>
    /// <param name="topic">
    ///     The topic name.
    /// </param>
    /// <returns>
    ///     The sum of the committed offsets.
    /// </returns>
    long GetCommittedOffsetsCount(string topic);

    /// <summary>
    ///     Waits until the consumers of the group have processed all the messages in the assigned partitions.
    /// </summary>
    /// <param name="topicNames">
    ///     The topics to be considered. All topics if empty.
    /// </param>
    /// <param name="cancellationToken">
    ///     A <see cref="CancellationToken" /> used to cancel the wait.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation.
    /// </returns>
    ValueTask WaitUntilAllMessagesAreConsumedAsync(IReadOnlyCollection<string> topicNames, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Waits until the consumer group has committed the offsets of all the messages in the assigned partitions.
    /// </summary>
    /// <param name="topicNames">
    ///     The topics to be considered. All topics if empty.
    /// </param>
    /// <param name="cancellationToken">
    ///     A <see cref="CancellationToken" /> used to cancel the wait.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation.
    /// </returns>
    ValueTask WaitUntilAllMessagesAreCommittedAsync(IReadOnlyCollection<string> topicNames, CancellationToken cancellationToken = default);
}
