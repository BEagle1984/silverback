// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Kafka.Mocks;
using Silverback.Messaging.Configuration;

namespace Silverback.Testing;

/// <inheritdoc cref="ITestingHelper{TBroker}" />
public interface IKafkaTestingHelper : ITestingHelper<KafkaBroker>
{
    /// <summary>
    ///     Returns the <see cref="IMockedConsumerGroup" /> representing the consumer group with the specified id.
    /// </summary>
    /// <remarks>
    ///     This method works with the mocked Kafka broker only. See <see cref="SilverbackBuilderUseMockedKafkaExtensions.UseMockedKafka" />
    ///     or <see cref="BrokerOptionsBuilderAddMockedKafkaExtensions.AddMockedKafka" />.
    /// </remarks>
    /// <param name="groupId">
    ///     The consumer group id.
    /// </param>
    /// <returns>
    ///     The <see cref="IMockedConsumerGroup" />.
    /// </returns>
    IMockedConsumerGroup GetConsumerGroup(string groupId);

    /// <summary>
    ///     Returns the <see cref="IMockedConsumerGroup" /> representing the consumer group with the specified id.
    /// </summary>
    /// <remarks>
    ///     This method works with the mocked Kafka broker only. See <see cref="SilverbackBuilderUseMockedKafkaExtensions.UseMockedKafka" />
    ///     or <see cref="BrokerOptionsBuilderAddMockedKafkaExtensions.AddMockedKafka" />.
    /// </remarks>
    /// <param name="groupId">
    ///     The consumer group id.
    /// </param>
    /// <param name="bootstrapServers">
    ///     The bootstrap servers string used to identify the target broker.
    /// </param>
    /// <returns>
    ///     The <see cref="IMockedConsumerGroup" />.
    /// </returns>
    IMockedConsumerGroup GetConsumerGroup(string groupId, string bootstrapServers);

    /// <summary>
    ///     Returns the <see cref="IInMemoryTopic" /> with the specified name.
    /// </summary>
    /// <remarks>
    ///     This method works with the mocked Kafka broker only. See <see cref="SilverbackBuilderUseMockedKafkaExtensions.UseMockedKafka" />
    ///     or <see cref="BrokerOptionsBuilderAddMockedKafkaExtensions.AddMockedKafka" />.
    /// </remarks>
    /// <param name="name">
    ///     The name of the topic.
    /// </param>
    /// <param name="bootstrapServers">
    ///     The bootstrap servers string used to identify the target broker. This must be specified when testing with
    ///     multiple brokers.
    /// </param>
    /// <returns>
    ///     The <see cref="IInMemoryTopic" />.
    /// </returns>
    IInMemoryTopic GetTopic(string name, string? bootstrapServers = null);
}
