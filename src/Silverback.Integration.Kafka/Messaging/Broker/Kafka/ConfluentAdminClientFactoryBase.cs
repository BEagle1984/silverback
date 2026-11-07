// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Confluent.Kafka;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Producing.EndpointResolvers;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Kafka;

/// <summary>
///     The base class for the <see cref="IConfluentAdminClientFactory" /> implementations. It resolves the configuration from the
///     configured clients and leaves the creation of the <see cref="IAdminClient" /> to the derived class.
/// </summary>
public abstract class ConfluentAdminClientFactoryBase : IConfluentAdminClientFactory
{
    private readonly IProducerCollection _producers;

    private readonly IConsumerCollection _consumers;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ConfluentAdminClientFactoryBase" /> class.
    /// </summary>
    /// <param name="producers">
    ///     The <see cref="IProducerCollection" />.
    /// </param>
    /// <param name="consumers">
    ///     The <see cref="IConsumerCollection" />.
    /// </param>
    protected ConfluentAdminClientFactoryBase(IProducerCollection producers, IConsumerCollection consumers)
    {
        _producers = Check.NotNull(producers, nameof(producers));
        _consumers = Check.NotNull(consumers, nameof(consumers));
    }

    /// <inheritdoc cref="IConfluentAdminClientFactory.GetClient(AdminClientConfig)" />
    public abstract IAdminClient GetClient(AdminClientConfig config);

    /// <inheritdoc cref="IConfluentAdminClientFactory.GetClient(Action{KafkaClientConfigurationBuilder})" />
    public IAdminClient GetClient(Action<KafkaClientConfigurationBuilder> builderAction)
    {
        Check.NotNull(builderAction, nameof(builderAction));

        KafkaClientConfigurationBuilder builder = new();
        builderAction.Invoke(builder);

        return GetClient(builder.Build());
    }

    /// <inheritdoc cref="IConfluentAdminClientFactory.GetClient(KafkaClientConfiguration)" />
    public IAdminClient GetClient(KafkaClientConfiguration configuration) =>
        GetClient(Check.NotNull(configuration, nameof(configuration)).ToConfluentAdminClientConfig());

    /// <inheritdoc cref="IConfluentAdminClientFactory.GetClient(KafkaProducerConfiguration)" />
    public IAdminClient GetClient(KafkaProducerConfiguration configuration) =>
        GetClient(Check.NotNull(configuration, nameof(configuration)).ToConfluentAdminClientConfig());

    /// <inheritdoc cref="IConfluentAdminClientFactory.GetClient(KafkaConsumerConfiguration)" />
    public IAdminClient GetClient(KafkaConsumerConfiguration configuration) =>
        GetClient(Check.NotNull(configuration, nameof(configuration)).ToConfluentAdminClientConfig());

    /// <inheritdoc cref="IConfluentAdminClientFactory.GetClient(IProducer)" />
    public IAdminClient GetClient(IProducer producer)
    {
        Check.NotNull(producer, nameof(producer));

        return producer switch
        {
            KafkaProducer kafkaProducer => GetClient(kafkaProducer.Configuration),
            KafkaTransactionalProducer kafkaTransactionalProducer => GetClient(kafkaTransactionalProducer.Configuration),
            _ => throw new ArgumentException("The producer is not a Kafka producer.", nameof(producer))
        };
    }

    /// <inheritdoc cref="IConfluentAdminClientFactory.GetClient(IConsumer)" />
    public IAdminClient GetClient(IConsumer consumer)
    {
        Check.NotNull(consumer, nameof(consumer));

        return consumer is IKafkaConsumer kafkaConsumer
            ? GetClient(kafkaConsumer.Configuration)
            : throw new ArgumentException("The consumer is not a Kafka consumer.", nameof(consumer));
    }

    /// <inheritdoc cref="IConfluentAdminClientFactory.GetClientForProducer" />
    public IAdminClient GetClientForProducer(string producerName)
    {
        Check.NotNull(producerName, nameof(producerName));

        IProducer producer =
            GetKafkaProducers().Find(kafkaProducer => IsNamed(kafkaProducer, producerName)) ??
            throw new InvalidOperationException($"No Kafka producer has been configured with name '{producerName}'.");

        return GetClient(producer);
    }

    /// <inheritdoc cref="IConfluentAdminClientFactory.GetClientForProducerEndpoint" />
    public IAdminClient GetClientForProducerEndpoint(string endpointName)
    {
        Check.NotNull(endpointName, nameof(endpointName));

        List<IProducer> kafkaProducers = GetKafkaProducers();

        IProducer producer =
            kafkaProducers.Find(kafkaProducer => kafkaProducer.EndpointConfiguration.FriendlyName == endpointName) ??
            kafkaProducers.Find(kafkaProducer => IsProducingTo(kafkaProducer.EndpointConfiguration, endpointName)) ??
            throw new InvalidOperationException($"No Kafka producer has been configured for endpoint '{endpointName}'.");

        return GetClient(producer);
    }

    /// <inheritdoc cref="IConfluentAdminClientFactory.GetClientForConsumer" />
    public IAdminClient GetClientForConsumer(string consumerName)
    {
        Check.NotNull(consumerName, nameof(consumerName));

        IKafkaConsumer consumer =
            _consumers.OfType<IKafkaConsumer>().FirstOrDefault(kafkaConsumer => kafkaConsumer.Name == consumerName) ??
            throw new InvalidOperationException($"No Kafka consumer has been configured with name '{consumerName}'.");

        return GetClient(consumer);
    }

    /// <inheritdoc cref="IConfluentAdminClientFactory.GetClientForConsumerEndpoint" />
    public IAdminClient GetClientForConsumerEndpoint(string endpointName)
    {
        Check.NotNull(endpointName, nameof(endpointName));

        List<IKafkaConsumer> kafkaConsumers = [.. _consumers.OfType<IKafkaConsumer>()];

        IKafkaConsumer consumer =
            kafkaConsumers.Find(kafkaConsumer => kafkaConsumer.EndpointsConfiguration.Any(endpoint => endpoint.FriendlyName == endpointName)) ??
            kafkaConsumers.Find(kafkaConsumer => kafkaConsumer.EndpointsConfiguration.Any(endpoint => IsConsumingFrom(endpoint, endpointName))) ??
            throw new InvalidOperationException($"No Kafka consumer has been configured for endpoint '{endpointName}'.");

        return GetClient(consumer);
    }

    // A KafkaProducer is created per endpoint with a generated name, while its client keeps the name specified when adding the producer
    private static bool IsNamed(IProducer producer, string name) =>
        producer is KafkaProducer kafkaProducer ? kafkaProducer.Client.Name == name : producer.Name == name;

    private static bool IsProducingTo(ProducerEndpointConfiguration endpoint, string name) =>
        endpoint.RawName == name ||
        endpoint.EndpointResolver is KafkaStaticProducerEndpointResolver resolver && resolver.TopicPartition.Topic == name;

    private static bool IsConsumingFrom(KafkaConsumerEndpointConfiguration endpoint, string name) =>
        endpoint.RawName == name || endpoint.TopicPartitions.Any(topicPartition => topicPartition.Topic == name);

    private List<IProducer> GetKafkaProducers() =>
        [.. _producers.Where(producer => producer is KafkaProducer or KafkaTransactionalProducer)];
}
