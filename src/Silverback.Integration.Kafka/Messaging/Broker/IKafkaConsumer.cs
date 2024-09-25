// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Confluent.Kafka;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Configuration.Kafka;

namespace Silverback.Messaging.Broker;

/// <inheritdoc cref="IConsumer" />
public interface IKafkaConsumer : IConsumer
{
    /// <inheritdoc cref="Consumer{TIdentifier}.Client" />
    new IConfluentConsumerWrapper Client { get; }

    /// <summary>
    ///     Gets the consumer configuration.
    /// </summary>
    KafkaConsumerConfiguration Configuration { get; }

    /// <inheritdoc cref="Consumer{TIdentifier}.EndpointsConfiguration" />
    new IReadOnlyCollection<KafkaConsumerEndpointConfiguration> EndpointsConfiguration { get; }

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
    ///     The list of <see cref="TopicPartition" /> to be paused.
    /// </param>
    void Resume(IEnumerable<TopicPartition> partitions);

    /// <summary>
    ///     Seeks the specified partition to the specified offset.
    /// </summary>
    /// <param name="topicPartitionOffset">
    ///     The offset.
    /// </param>
    void Seek(TopicPartitionOffset topicPartitionOffset);

    /// <summary>
    ///     Looks up the offsets for the given partitions by timestamp. The returned offset for each partition is the earliest offset for which
    ///     the timestamp is greater than or equal to the given timestamp. If the provided timestamp exceeds that of the last message in the
    ///     partition, a value of <see cref="Offset.End" /> will be returned.
    /// </summary>
    /// <remarks>
    ///     The consumer does not need to be assigned to the requested partitions.
    /// </remarks>
    /// <param name="topicPartitionTimestamps">
    ///     The mapping from partition to the timestamp to look up.
    /// </param>
    /// <param name="timeout">
    ///     The maximum period of time the call may block.
    /// </param>
    /// <returns>
    ///     A mapping from partition to the timestamp and offset of the first message with timestamp greater than or equal to the target timestamp.
    /// </returns>
    IReadOnlyList<TopicPartitionOffset> GetOffsetsForTimestamps(IEnumerable<TopicPartitionTimestamp> topicPartitionTimestamps, TimeSpan timeout);
}
