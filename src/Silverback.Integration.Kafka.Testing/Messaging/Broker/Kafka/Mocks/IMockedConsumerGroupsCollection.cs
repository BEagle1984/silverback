// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Confluent.Kafka;

namespace Silverback.Messaging.Broker.Kafka.Mocks
{
    /// <summary>
    ///     The collection of <see cref="MockedConsumerGroup" /> being used in the current session.
    /// </summary>
    public interface IMockedConsumerGroupsCollection : IReadOnlyCollection<IMockedConsumerGroup>
    {
        /// <summary>
        ///     Gets the topic with the specified name or creates it on the fly.
        /// </summary>
        /// <param name="consumerConfig">
        ///     The consumer configuration.
        /// </param>
        /// <returns>
        ///     The in-memory topic.
        /// </returns>
        IMockedConsumerGroup Get(ConsumerConfig consumerConfig);

        /// <summary>
        ///     Gets the topic with the specified name or creates it on the fly.
        /// </summary>
        /// <param name="name">
        ///     The name of the consumer group.
        /// </param>
        /// <param name="bootstrapServers">
        ///     The bootstrap servers string used to identify the target broker.
        /// </param>
        /// <returns>
        ///     The in-memory topic.
        /// </returns>
        IMockedConsumerGroup Get(string name, string bootstrapServers);
    }
}
