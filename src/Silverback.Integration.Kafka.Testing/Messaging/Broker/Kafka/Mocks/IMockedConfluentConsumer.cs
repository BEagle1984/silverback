// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;

namespace Silverback.Messaging.Broker.Kafka.Mocks
{
    /// <summary>
    ///     A mocked implementation of <see cref="IConsumer{TKey,TValue}" /> from Confluent.Kafka that consumes
    ///     from an <see cref="IInMemoryTopic" />.
    /// </summary>
    public interface IMockedConfluentConsumer : IConsumer<byte[]?, byte[]?>
    {
        /// <summary>
        ///     Gets the consumer group id.
        /// </summary>
        string GroupId { get; }

        /// <summary>
        ///     Gets a value indicating whether the partition EOF event has to be emitted.
        /// </summary>
        bool EnablePartitionEof { get; }

        /// <summary>
        ///     Gets a value indicating whether the partitions have been assigned to the consumer.
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
    }
}
