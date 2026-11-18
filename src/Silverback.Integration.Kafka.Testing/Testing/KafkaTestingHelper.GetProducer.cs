// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Util;

namespace Silverback.Testing;

/// <content>
///     Implements the <c>GetProducer</c> methods.
/// </content>
public partial class KafkaTestingHelper
{
    /// <inheritdoc cref="IKafkaTestingHelper.GetProducer" />
    public IProducer GetProducer(Action<KafkaProducerConfigurationBuilder> configurationBuilderAction)
    {
        Check.NotNull(configurationBuilderAction);

        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        KafkaProducersInitializer producersInitializer = scope.ServiceProvider.GetService<KafkaProducersInitializer>() ??
                                                         throw new InvalidOperationException("The KafkaProducersInitializer is not initialized.");
        KafkaProducerConfigurationBuilder builder = new(_serviceProvider);
        configurationBuilderAction.Invoke(builder);
        KafkaProducerConfiguration producerConfiguration = builder.Build();

        IProducer newProducer = producersInitializer
            .InitializeProducers($"test-{Guid.NewGuid():N}", producerConfiguration, false)
            .Single();

        if (newProducer is KafkaProducer kafkaProducer)
            kafkaProducer.Client.ConnectAsync().SafeWait();

        return newProducer;
    }

    /// <inheritdoc cref="TestingHelper.GetProducerForConsumer" />
    protected override IProducer? GetProducerForConsumer(IConsumerCollection consumers, string endpointName)
    {
        List<(KafkaConsumerConfiguration ConsumerConfiguration, KafkaConsumerEndpointConfiguration EndpointConfiguration)> endpoints =
        [
            .. consumers
                .OfType<IKafkaConsumer>()
                .SelectMany(consumer => consumer.Configuration.Endpoints.Select(endpoint => (consumer.Configuration, endpoint)))
        ];

        // The friendly name takes precedence over the topic name
        (KafkaConsumerConfiguration? namedConsumerConfiguration, KafkaConsumerEndpointConfiguration? namedEndpointConfiguration) =
            endpoints.FirstOrDefault(tuple => tuple.EndpointConfiguration.FriendlyName == endpointName);

        if (namedConsumerConfiguration == null || namedEndpointConfiguration == null)
        {
            (namedConsumerConfiguration, namedEndpointConfiguration) =
                endpoints.FirstOrDefault(tuple => tuple.EndpointConfiguration.RawName == endpointName);
        }

        if (namedConsumerConfiguration != null && namedEndpointConfiguration != null)
            return CreateProducerForNamedEndpoint(namedConsumerConfiguration, namedEndpointConfiguration, endpointName);

        // A topic name is resolved to the endpoint subscribing it, giving precedence to the names over the patterns (a pattern can't be a
        // topic name)
        (KafkaConsumerConfiguration? consumerConfiguration, KafkaConsumerEndpointConfiguration? endpointConfiguration) =
            endpoints.FirstOrDefault(tuple =>
                IsSubscribing(tuple.ConsumerConfiguration, tuple.EndpointConfiguration, endpointName, false));

        if ((consumerConfiguration == null || endpointConfiguration == null) && !KafkaTopicPattern.IsPattern(endpointName))
        {
            (consumerConfiguration, endpointConfiguration) =
                endpoints.FirstOrDefault(tuple =>
                    IsSubscribing(tuple.ConsumerConfiguration, tuple.EndpointConfiguration, endpointName, true));
        }

        if (consumerConfiguration != null && endpointConfiguration != null)
        {
            // Produce to the configured partition, if the topic is statically assigned
            TopicPartition topicPartition =
                endpointConfiguration.TopicPartitions.FirstOrDefault(topicPartition => topicPartition.Topic == endpointName)?.TopicPartition ??
                new TopicPartition(endpointName, Partition.Any);

            return CreateProducer(consumerConfiguration, endpointConfiguration, topicPartition);
        }

        if (KafkaTopicPattern.IsPattern(endpointName))
        {
            throw new InvalidOperationException(
                $"Cannot produce to '{endpointName}', since it is a topic pattern. " +
                "Get a producer for one of the matching topics instead, specifying its name.");
        }

        return null;
    }

    private static bool IsSubscribing(
        KafkaConsumerConfiguration consumerConfiguration,
        KafkaConsumerEndpointConfiguration endpointConfiguration,
        string topic,
        bool isPattern) =>
        endpointConfiguration.TopicPartitions.Any(topicPartition =>
            KafkaTopicPattern.IsPattern(topicPartition.Topic) == isPattern &&
            KafkaTopicPattern.IsMatch(topicPartition.Topic, topic, KafkaTopicPattern.IsFullMatchRequired(consumerConfiguration.GroupProtocol)));

    private static void MapEndpoint(
        KafkaProducerEndpointConfigurationBuilder<object> endpoint,
        KafkaConsumerEndpointConfiguration endpointConfiguration,
        TopicPartition topicPartition)
    {
        endpoint.MirrorConsumerEndpoint(endpointConfiguration);
        endpoint.ProduceTo(topicPartition);
    }

    private IProducer CreateProducerForNamedEndpoint(
        KafkaConsumerConfiguration consumerConfiguration,
        KafkaConsumerEndpointConfiguration endpointConfiguration,
        string endpointName)
    {
        TopicPartitionOffset? topicPartition =
            endpointConfiguration.TopicPartitions.FirstOrDefault(topicPartition => !KafkaTopicPattern.IsPattern(topicPartition.Topic));

        if (topicPartition != null)
            return CreateProducer(consumerConfiguration, endpointConfiguration, topicPartition.TopicPartition);

        // The friendly name can be a topic matching the endpoint's own patterns, but it must not be resolved to another endpoint
        if (!KafkaTopicPattern.IsPattern(endpointName) && IsSubscribing(consumerConfiguration, endpointConfiguration, endpointName, true))
            return CreateProducer(consumerConfiguration, endpointConfiguration, new TopicPartition(endpointName, Partition.Any));

        throw new InvalidOperationException(
            $"Cannot produce to the endpoint '{endpointConfiguration.DisplayName}', since it only subscribes topic patterns. " +
            "Get a producer for one of the matching topics instead, specifying its name.");
    }

    private IProducer CreateProducer(
        KafkaConsumerConfiguration consumerConfiguration,
        KafkaConsumerEndpointConfiguration endpointConfiguration,
        TopicPartition topicPartition) =>
        GetProducer(producer => producer
            .WithBootstrapServers(consumerConfiguration.BootstrapServers)
            .Produce<object>(endpoint => MapEndpoint(endpoint, endpointConfiguration, topicPartition)));
}
