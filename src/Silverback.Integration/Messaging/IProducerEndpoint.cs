// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Outbound;
using Silverback.Messaging.Sequences.Chunking;

namespace Silverback.Messaging
{
    /// <summary>
    ///     Represents a message broker endpoint to produce to (such as a Kafka topic or RabbitMQ queue or
    ///     exchange).
    /// </summary>
    public interface IProducerEndpoint : IEndpoint
    {
        /// <summary>
        ///     Gets the message chunking settings. This option can be used to split large messages into smaller
        ///     chunks.
        /// </summary>
        ChunkSettings? Chunk { get; }

        /// <summary>
        ///     Gets the strategy to be used to produce the messages. If no strategy is specified, the
        ///     messages will be sent to the message broker directly.
        /// </summary>
        IProduceStrategy? Strategy { get; }
    }
}
