// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.LargeMessages;

namespace Silverback.Messaging
{
    /// <summary>
    ///     Represents a message broker endpoint to produce to (such as a Kafka topic or RabbitMQ queue or exchange).
    /// </summary>
    public interface IProducerEndpoint : IEndpoint
    {
        /// <summary>
        ///     Gets the message chunking settings. This option can be used to split large messages
        ///     into smaller chunks.
        /// </summary>
        ChunkSettings Chunk { get; }
    }
}