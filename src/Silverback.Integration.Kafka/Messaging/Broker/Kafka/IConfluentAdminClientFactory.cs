// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.Kafka;
using Silverback.Messaging.Configuration.Kafka;

namespace Silverback.Messaging.Broker.Kafka;

/// <summary>
///     The factory used to create <see cref="IAdminClient" /> instances. Each method returns a new instance, which must be disposed by
///     the caller.
/// </summary>
public interface IConfluentAdminClientFactory
{
    /// <summary>
    ///     Returns an <see cref="IAdminClient" /> instance for the specified configuration.
    /// </summary>
    /// <param name="config">
    ///     The client configuration.
    /// </param>
    /// <returns>
    ///     The <see cref="IAdminClient" />.
    /// </returns>
    IAdminClient GetClient(AdminClientConfig config);

    /// <summary>
    ///     Returns an <see cref="IAdminClient" /> instance using the specified configuration.
    /// </summary>
    /// <param name="builderAction">
    ///     An <see cref="Action{T}" /> that takes a <see cref="KafkaClientConfigurationBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="IAdminClient" />.
    /// </returns>
    IAdminClient GetClient(Action<KafkaClientConfigurationBuilder> builderAction);

    /// <summary>
    ///     Returns an <see cref="IAdminClient" /> instance using the specified configuration.
    /// </summary>
    /// <param name="configuration">
    ///     The configuration.
    /// </param>
    /// <returns>
    ///     The <see cref="IAdminClient" />.
    /// </returns>
    IAdminClient GetClient(KafkaClientConfiguration configuration);

    /// <summary>
    ///     Returns an <see cref="IAdminClient" /> instance using the specified configuration.
    /// </summary>
    /// <param name="configuration">
    ///     The configuration.
    /// </param>
    /// <returns>
    ///     The <see cref="IAdminClient" />.
    /// </returns>
    IAdminClient GetClient(KafkaProducerConfiguration configuration);

    /// <summary>
    ///     Returns an <see cref="IAdminClient" /> instance using the specified configuration.
    /// </summary>
    /// <param name="configuration">
    ///     The configuration.
    /// </param>
    /// <returns>
    ///     The <see cref="IAdminClient" />.
    /// </returns>
    IAdminClient GetClient(KafkaConsumerConfiguration configuration);

    /// <summary>
    ///     Returns an <see cref="IAdminClient" /> instance using the configuration of the specified producer.
    /// </summary>
    /// <param name="producer">
    ///     The Kafka producer.
    /// </param>
    /// <returns>
    ///     The <see cref="IAdminClient" />.
    /// </returns>
    IAdminClient GetClient(IProducer producer);

    /// <summary>
    ///     Returns an <see cref="IAdminClient" /> instance using the configuration of the specified consumer.
    /// </summary>
    /// <param name="consumer">
    ///     The Kafka consumer.
    /// </param>
    /// <returns>
    ///     The <see cref="IAdminClient" />.
    /// </returns>
    IAdminClient GetClient(IConsumer consumer);

    /// <summary>
    ///     Returns an <see cref="IAdminClient" /> instance using the configuration of the Kafka producer with the specified name.
    /// </summary>
    /// <param name="producerName">
    ///     The name specified when adding the producer.
    /// </param>
    /// <returns>
    ///     The <see cref="IAdminClient" />.
    /// </returns>
    IAdminClient GetClientForProducer(string producerName);

    /// <summary>
    ///     Returns an <see cref="IAdminClient" /> instance using the configuration of the Kafka producer for the specified endpoint.
    /// </summary>
    /// <param name="endpointName">
    ///     The endpoint friendly name or the topic name. The endpoints with a dynamic target topic can be found by friendly name only.
    /// </param>
    /// <returns>
    ///     The <see cref="IAdminClient" />.
    /// </returns>
    IAdminClient GetClientForProducerEndpoint(string endpointName);

    /// <summary>
    ///     Returns an <see cref="IAdminClient" /> instance using the configuration of the Kafka consumer with the specified name.
    /// </summary>
    /// <param name="consumerName">
    ///     The name specified when adding the consumer.
    /// </param>
    /// <returns>
    ///     The <see cref="IAdminClient" />.
    /// </returns>
    IAdminClient GetClientForConsumer(string consumerName);

    /// <summary>
    ///     Returns an <see cref="IAdminClient" /> instance using the configuration of the Kafka consumer for the specified endpoint.
    /// </summary>
    /// <param name="endpointName">
    ///     The endpoint friendly name or the name of one of the consumed topics.
    /// </param>
    /// <returns>
    ///     The <see cref="IAdminClient" />.
    /// </returns>
    IAdminClient GetClientForConsumerEndpoint(string endpointName);
}
