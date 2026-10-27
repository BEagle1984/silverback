// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Util;

namespace Silverback.Messaging.Broker.Kafka;

/// <summary>
///     Represents an offset in a Kafka topic partition. Same shape as the type of the Confluent.Kafka based integration, so that the code written
///     against it compiles unchanged, without exposing the underlying client library.
/// </summary>
public sealed record TopicPartitionOffset
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TopicPartitionOffset" /> class.
    /// </summary>
    /// <param name="topicPartition">
    ///     The topic partition.
    /// </param>
    /// <param name="offset">
    ///     The offset.
    /// </param>
    public TopicPartitionOffset(TopicPartition topicPartition, Offset offset)
    {
        TopicPartition = Check.NotNull(topicPartition, nameof(topicPartition));
        Offset = offset;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="TopicPartitionOffset" /> class.
    /// </summary>
    /// <param name="topic">
    ///     The topic name.
    /// </param>
    /// <param name="partition">
    ///     The partition.
    /// </param>
    /// <param name="offset">
    ///     The offset.
    /// </param>
    public TopicPartitionOffset(string topic, Partition partition, Offset offset)
        : this(new TopicPartition(topic, partition), offset)
    {
    }

    /// <summary>
    ///     Gets the topic partition.
    /// </summary>
    public TopicPartition TopicPartition { get; }

    /// <summary>
    ///     Gets the topic name.
    /// </summary>
    public string Topic => TopicPartition.Topic;

    /// <summary>
    ///     Gets the partition.
    /// </summary>
    public Partition Partition => TopicPartition.Partition;

    /// <summary>
    ///     Gets the offset.
    /// </summary>
    public Offset Offset { get; }

    /// <inheritdoc cref="object.ToString" />
    public override string ToString() => $"{TopicPartition} @{Offset}";
}
