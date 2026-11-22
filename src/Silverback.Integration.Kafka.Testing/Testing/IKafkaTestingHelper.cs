// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Kafka.Mocks;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;

namespace Silverback.Testing;

/// <inheritdoc cref="ITestingHelper" />
public partial interface IKafkaTestingHelper : ITestingHelper
{
    /// <summary>
    ///     Gets a collection of <see cref="IMockedConsumerGroup" /> representing all known consumer groups.
    /// </summary>
    /// <returns>
    ///     The collection of <see cref="IMockedConsumerGroup" />.
    /// </returns>
    IReadOnlyCollection<IMockedConsumerGroup> ConsumerGroups { get; }

    /// <summary>
    ///     Returns the <see cref="IMockedConsumerGroup" /> representing the consumer group with the specified id.
    /// </summary>
    /// <remarks>
    ///     This method works with the mocked Kafka broker only. See <see cref="SilverbackBuilderKafkaTestingExtensions.UseMockedKafka" />
    ///     or <see cref="BrokerOptionsBuilderKafkaTestingExtensions.AddMockedKafka" />.
    /// </remarks>
    /// <param name="groupId">
    ///     The consumer group id.
    /// </param>
    /// <returns>
    ///     The <see cref="IMockedConsumerGroup" />.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///     No consumer group with the specified id exists.
    /// </exception>
    IMockedConsumerGroup GetConsumerGroup(string groupId);

    /// <summary>
    ///     Returns the <see cref="IMockedConsumerGroup" /> representing the consumer group with the specified id.
    /// </summary>
    /// <remarks>
    ///     This method works with the mocked Kafka broker only. See <see cref="SilverbackBuilderKafkaTestingExtensions.UseMockedKafka" />
    ///     or <see cref="BrokerOptionsBuilderKafkaTestingExtensions.AddMockedKafka" />.
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
    /// <exception cref="InvalidOperationException">
    ///     No consumer group with the specified id exists for the specified bootstrap servers.
    /// </exception>
    IMockedConsumerGroup GetConsumerGroup(string groupId, string bootstrapServers);

    /// <summary>
    ///     Returns the <see cref="IInMemoryTopic" /> with the specified name.
    /// </summary>
    /// <remarks>
    ///     This method works with the mocked Kafka broker only. See <see cref="SilverbackBuilderKafkaTestingExtensions.UseMockedKafka" />
    ///     or <see cref="BrokerOptionsBuilderKafkaTestingExtensions.AddMockedKafka" />.
    /// </remarks>
    /// <param name="name">
    ///     The name of the topic. Must not be empty.
    /// </param>
    /// <param name="bootstrapServers">
    ///     The bootstrap servers string used to identify the target broker. This must be specified when testing with
    ///     multiple brokers. If specified, it must not be empty.
    /// </param>
    /// <returns>
    ///     The <see cref="IInMemoryTopic" />.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///     The <paramref name="bootstrapServers" /> aren't specified and either more than one topic with the specified name exists, or
    ///     the topic doesn't exist and the existing topics don't belong to exactly one broker (for example, because no topic exists
    ///     yet).
    /// </exception>
    IInMemoryTopic GetTopic(string name, string? bootstrapServers = null);

    /// <summary>
    ///     Gets a new producer with the specified configuration.
    /// </summary>
    /// <param name="configurationBuilderAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="KafkaProducerConfigurationBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="IProducer" />.
    /// </returns>
    /// <exception cref="BrokerConfigurationException">
    ///     The producer configuration is not valid.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     The Kafka integration hasn't been added (<c>AddKafka</c> hasn't been called), the configuration defines more than one
    ///     producer endpoint, or a producer endpoint with the same name has already been added.
    /// </exception>
    IProducer GetProducer(Action<KafkaProducerConfigurationBuilder> configurationBuilderAction);
}
