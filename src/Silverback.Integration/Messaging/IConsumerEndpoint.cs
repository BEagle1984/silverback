// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging
{
    /// <summary>
    ///     Represents a message broker endpoint such as a Kafka topic or RabbitMQ queue or exchange.
    /// </summary>
    public interface IConsumerEndpoint : IEndpoint
    {
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
