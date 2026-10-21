// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Dekaf.Consumer;
using Silverback.Messaging.Configuration.Kafka;

namespace Silverback.Messaging.Broker.Kafka;

/// <summary>
///     Wraps the underlying Dekaf consumer, exposing the synchronous operations the Silverback consume loop relies on,
///     and handles the connection lifecycle.
/// </summary>
internal interface IDekafConsumerWrapper : IBrokerClient
{
    /// <summary>
    ///     Gets the consumer configuration.
    /// </summary>
    KafkaConsumerConfiguration Configuration { get; }

    /// <summary>
    ///     Gets the list of partitions currently assigned to this consumer.
    /// </summary>
    IReadOnlyList<TopicPartition> Assignment { get; }

    /// <summary>
    ///     Gets or sets the related consumer instance.
    /// </summary>
    KafkaConsumer Consumer { get; set; }

    /// <summary>
    ///     Gets the consumer group metadata, to be sent to the producer transaction together with the offsets.
    /// </summary>
    /// <returns>
    ///     The <see cref="ConsumerGroupMetadata" />.
    /// </returns>
    ConsumerGroupMetadata GetConsumerGroupMetadata();

    /// <summary>
    ///     Polls for the next record. The call blocks until a record is available, the timeout elapses or the operation
    ///     is canceled.
    /// </summary>
    /// <param name="timeout">
    ///     The maximum period of time the call may block.
    /// </param>
    /// <returns>
    ///     The next <see cref="KafkaConsumedRecord" />, which may also signal the end of a partition, or <c>null</c> if
    ///     no record was received within the timeout.
    /// </returns>
    KafkaConsumedRecord? Consume(TimeSpan timeout);

    /// <summary>
    ///     Stores the specified offset for the specified partition. <br />
    ///     The offset will be committed (written) to the offset store according to
    ///     <see cref="KafkaConsumerConfiguration.AutoCommitIntervalMs" /> or with a call to the <see cref="Commit" />
    ///     method.
    /// </summary>
    /// <param name="topicPartitionOffset">
    ///     The offset to be stored.
    /// </param>
    void StoreOffset(TopicPartitionOffset topicPartitionOffset);

    /// <summary>
    ///     Commits all stored offsets. Kafka commit failures are logged without being rethrown.
    /// </summary>
    void Commit();

    /// <summary>
    ///     Pauses the consumption of the specified partitions.
    /// </summary>
    /// <param name="partitions">
    ///     The list of <see cref="TopicPartition" /> to be paused.
    /// </param>
    void Pause(IEnumerable<TopicPartition> partitions);

    /// <summary>
    ///     Resumes the consumption of the specified partitions.
    /// </summary>
    /// <param name="partitions">
    ///     The list of <see cref="TopicPartition" /> to be resumed.
    /// </param>
    void Resume(IEnumerable<TopicPartition> partitions);

    /// <summary>
    ///     Seeks the specified partition to the specified offset. <see cref="Offset.Unset" /> and
    ///     <see cref="Offset.Stored" /> are ignored, since they don't identify a position to seek to.
    /// </summary>
    /// <param name="topicPartitionOffset">
    ///     The offset.
    /// </param>
    void Seek(TopicPartitionOffset topicPartitionOffset);

    /// <summary>
    ///     Looks up the offsets for the given partitions by timestamp. The returned offset for each partition is the
    ///     earliest offset for which the timestamp is greater than or equal to the given timestamp.
    /// </summary>
    /// <param name="timestampsToSearch">
    ///     The mapping from partition to the timestamp to look up.
    /// </param>
    /// <param name="timeout">
    ///     The maximum period of time the call may block.
    /// </param>
    /// <returns>
    ///     The offset of the first message with timestamp greater than or equal to the target timestamp, for each
    ///     partition.
    /// </returns>
    IReadOnlyList<TopicPartitionOffset> OffsetsForTimes(IEnumerable<TopicPartitionTimestamp> timestampsToSearch, TimeSpan timeout);
}
