// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging
{
    public interface IConsumerEndpoint : IEndpoint
    {
        /// <summary>
        ///     Gets a unique name for the consumer group (e.g. Kafka's consumer group id).
        ///     This value will be used for example to ensure the exactly-once delivery.
        /// </summary>
        /// <remarks>
        ///     It's not enough to return the endpoint name, since the same topic could be consumed by
        ///     multiple consumer groups within the same process and/or using the same database to store
        ///     the information needed for the exactly-once delivery.
        /// </remarks>
        string GetUniqueConsumerGroupName();
    }
}