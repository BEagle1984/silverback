// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Kafka;

/// <summary>
///     Represents a timestamp in a Kafka topic partition, used to look up the first offset at or after it.
/// </summary>
public sealed record TopicPartitionTimestamp
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TopicPartitionTimestamp" /> class.
    /// </summary>
    /// <param name="topicPartition">
    ///     The topic partition.
    /// </param>
    /// <param name="timestamp">
    ///     The timestamp.
    /// </param>
    public TopicPartitionTimestamp(TopicPartition topicPartition, DateTimeOffset timestamp)
    {
        TopicPartition = Check.NotNull(topicPartition, nameof(topicPartition));
        Timestamp = timestamp;
    }

    /// <summary>
    ///     Gets the topic partition.
    /// </summary>
    public TopicPartition TopicPartition { get; }

    /// <summary>
    ///     Gets the timestamp.
    /// </summary>
    public DateTimeOffset Timestamp { get; }
}
