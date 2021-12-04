// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;

namespace Silverback.Messaging.Broker.Kafka.Mocks;

/// <summary>
///     A mocked implementation of <see cref="IConsumer{TKey,TValue}" /> from Confluent.Kafka that consumes
///     from an <see cref="IInMemoryTopic" />.
/// </summary>
public interface IMockedConfluentConsumer : IConsumer<byte[]?, byte[]?>
{
    /// <summary>
    ///     Gets the <see cref="ConsumerConfig" />.
    /// </summary>
    public ConsumerConfig Config { get; }

    /// <summary>
    ///     Gets a value indicating whether the partitions have been assigned to the consumer, either manually or via
    ///     rebalance.
    /// </summary>
    /// <remarks>
    ///     This value indicates that the rebalance process is over. It could be that no partition has actually
    ///     been assigned.
    /// </remarks>
    bool PartitionsAssigned { get; }

    /// <summary>
    ///     Gets a value indicating whether this instance was disposed.
    /// </summary>
    bool Disposed { get; }

    /// <summary>
    ///     Gets the stored offset for the specified topic partition.
    /// </summary>
    /// <param name="topicPartition">
    ///     The topic partition.
    /// </param>
    /// <returns>
    ///     The <see cref="TopicPartitionOffset" /> representing the offset, or <see cref="Offset.Unset" /> if the specified
    ///     topic partition isn't currently assigned to the consumer or no offset has been stored so far.
    /// </returns>
    TopicPartitionOffset GetStoredOffset(TopicPartition topicPartition);
}
