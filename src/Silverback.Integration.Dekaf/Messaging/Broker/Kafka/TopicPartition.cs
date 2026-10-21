// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Kafka;

/// <summary>
///     Represents a Kafka topic partition. Same shape as the type of the Confluent.Kafka based integration, so that the code written
///     against it compiles unchanged, without exposing the underlying client library.
/// </summary>
public sealed record TopicPartition : IComparable<TopicPartition>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TopicPartition" /> class.
    /// </summary>
    /// <param name="topic">
    ///     The topic name.
    /// </param>
    /// <param name="partition">
    ///     The partition.
    /// </param>
    public TopicPartition(string topic, Partition partition)
    {
        Topic = Check.NotNull(topic, nameof(topic));
        Partition = partition;
    }

    /// <summary>
    ///     Gets the topic name.
    /// </summary>
    public string Topic { get; }

    /// <summary>
    ///     Gets the partition.
    /// </summary>
    public Partition Partition { get; }

    /// <summary>
    ///     Compares two topic partitions.
    /// </summary>
    /// <param name="left">
    ///     The left operand.
    /// </param>
    /// <param name="right">
    ///     The right operand.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the left topic partition sorts before the right one.
    /// </returns>
    public static bool operator <(TopicPartition? left, TopicPartition? right) => Compare(left, right) < 0;

    /// <summary>
    ///     Compares two topic partitions.
    /// </summary>
    /// <param name="left">
    ///     The left operand.
    /// </param>
    /// <param name="right">
    ///     The right operand.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the left topic partition sorts after the right one.
    /// </returns>
    public static bool operator >(TopicPartition? left, TopicPartition? right) => Compare(left, right) > 0;

    /// <summary>
    ///     Compares two topic partitions.
    /// </summary>
    /// <param name="left">
    ///     The left operand.
    /// </param>
    /// <param name="right">
    ///     The right operand.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the left topic partition sorts before or equal to the right one.
    /// </returns>
    public static bool operator <=(TopicPartition? left, TopicPartition? right) => Compare(left, right) <= 0;

    /// <summary>
    ///     Compares two topic partitions.
    /// </summary>
    /// <param name="left">
    ///     The left operand.
    /// </param>
    /// <param name="right">
    ///     The right operand.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the left topic partition sorts after or equal to the right one.
    /// </returns>
    public static bool operator >=(TopicPartition? left, TopicPartition? right) => Compare(left, right) >= 0;

    /// <inheritdoc cref="IComparable{T}.CompareTo" />
    public int CompareTo(TopicPartition? other)
    {
        if (other is null)
            return 1;

        int result = string.CompareOrdinal(Topic, other.Topic);
        return result != 0 ? result : Partition.CompareTo(other.Partition);
    }

    /// <inheritdoc cref="object.ToString" />
    public override string ToString() => $"{Topic} {Partition}";

    private static int Compare(TopicPartition? left, TopicPartition? right) =>
        left is null ? (right is null ? 0 : -1) : left.CompareTo(right);
}
