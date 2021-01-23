// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;
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
        IProduceStrategy Strategy { get; }

        /// <summary>
        ///     Gets the actual target endpoint name for the message being produced.
        /// </summary>
        /// <param name="envelope">
        ///     The envelope containing the message being produced.
        /// </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider"/> in the current scope.
        /// </param>
        /// <returns>
        ///     The actual name of the endpoint to be produced to.
        /// </returns>
        public string GetActualName(IOutboundEnvelope envelope, IServiceProvider serviceProvider);
    }
}
