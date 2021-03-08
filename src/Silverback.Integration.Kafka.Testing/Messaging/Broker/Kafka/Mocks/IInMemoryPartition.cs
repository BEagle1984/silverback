// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Confluent.Kafka;

namespace Silverback.Messaging.Broker.Kafka.Mocks
{
    /// <summary>
    ///     A mocked topic partition where the messages are just stored in memory.
    /// </summary>
    public interface IInMemoryPartition
    {
        /// <summary>
        ///     Gets the <see cref="Partition"/> (index).
        /// </summary>
        Partition Partition { get; }

        /// <summary>
        ///     Gets the <see cref="Offset"/> of the first message in the partition.
        /// </summary>
        Offset FirstOffset { get; }

        /// <summary>
        ///     Gets the <see cref="Offset"/> of the latest message in the partition.
        /// </summary>
        Offset LastOffset { get; }

        /// <summary>
        ///     Gets the messages written to the partition.
        /// </summary>
        IReadOnlyCollection<Message<byte[]?, byte[]?>> Messages { get; }
    }
}
