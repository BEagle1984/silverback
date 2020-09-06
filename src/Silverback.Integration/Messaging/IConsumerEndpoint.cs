// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Batch;
using Silverback.Messaging.ErrorHandling;

namespace Silverback.Messaging
{
    /// <summary>
    ///     Represents a message broker endpoint to consume from (such as a Kafka topic or RabbitMQ queue or
    ///     exchange).
    /// </summary>
    public interface IConsumerEndpoint : IEndpoint
    {
        IErrorPolicy? ErrorPolicy { get; }

        /// <summary>
        ///     Gets the batch settings. Can be used to enable and setup batch processing.
        /// </summary>
        BatchSettings Batch { get; }

        /// <summary>
        ///     Gets a value indicating whether an exception must be thrown if no subscriber is handling the
        ///     received message. The default is <c>false</c> and it means that the unhandled messages are silently
        ///     discarded.
        /// </summary>
        bool ThrowIfUnhandled { get; }

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
