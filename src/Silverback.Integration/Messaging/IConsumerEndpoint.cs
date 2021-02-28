// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Inbound.ErrorHandling;
using Silverback.Messaging.Inbound.ExactlyOnce;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Sequences.Batch;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging
{
    /// <summary>
    ///     Represents a message broker endpoint to consume from (such as a Kafka topic or RabbitMQ queue or
    ///     exchange).
    /// </summary>
    public interface IConsumerEndpoint : IEndpoint
    {
        /// <summary>
        ///     Gets the batch settings. Can be used to enable and setup batch processing.
        /// </summary>
        BatchSettings? Batch { get; }

        /// <summary>
        ///     Gets the sequence settings. A sequence is a set of related messages, like the chunks belonging to the
        ///     same message or the messages in a dataset.
        /// </summary>
        SequenceSettings Sequence { get; }

        /// <summary>
        ///     Gets a value indicating whether an exception must be thrown if no subscriber is handling the
        ///     received message. The default is <c>true</c>.
        /// </summary>
        bool ThrowIfUnhandled { get; }

        /// <summary>
        ///     Gets the error policy to be applied when an exception occurs during the processing of the
        ///     consumed messages.
        /// </summary>
        IErrorPolicy ErrorPolicy { get; }

        /// <summary>
        ///     Gets the strategy to be used to guarantee that each message is consumed only once.
        /// </summary>
        IExactlyOnceStrategy? ExactlyOnceStrategy { get; }

        /// <summary>
        ///     Gets a value indicating how to handle the null messages. The default value is
        ///     <see cref="Serialization.NullMessageHandlingStrategy.Tombstone" />.
        /// </summary>
        NullMessageHandlingStrategy NullMessageHandlingStrategy { get; }

        /// <summary>
        ///     Gets a unique name for the consumer group (e.g. Kafka's consumer group id). This value (joint with
        ///     the endpoint name) will be used for example to ensure the exactly-once delivery.
        /// </summary>
        /// <remarks>
        ///     It's not enough to use the endpoint name, since the same topic could be consumed by multiple
        ///     consumer groups within the same process and/or using the same database to store the information
        ///     needed to ensure the exactly-once delivery.
        /// </remarks>
        /// <returns>
        ///     Returns the unique name for the consumer group.
        /// </returns>
        string GetUniqueConsumerGroupName();
    }
}
