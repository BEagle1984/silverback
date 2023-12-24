// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
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
        Check.NotNull(configurationBuilderAction, nameof(configurationBuilderAction));

        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        KafkaProducersInitializer producersInitializer = scope.ServiceProvider.GetService<KafkaProducersInitializer>() ??
                                                         throw new InvalidOperationException("The KafkaProducersInitializer is not initialized.");
        KafkaProducerConfigurationBuilder builder = new();
        configurationBuilderAction.Invoke(builder);
        KafkaProducerConfiguration producerConfiguration = builder.Build();

        IProducer newProducer = producersInitializer
            .InitializeProducers($"test-{Guid.NewGuid():N}", producerConfiguration, false)
            .Single();

        if (newProducer is KafkaProducer kafkaProducer)
            AsyncHelper.RunSynchronously(kafkaProducer.Client.ConnectAsync);

        return newProducer;
    }

    /// <inheritdoc cref="TestingHelper.GetProducerForConsumer" />
    protected override IProducer? GetProducerForConsumer(IConsumerCollection consumers, string endpointName)
    {
        (KafkaConsumerConfiguration? consumerConfiguration, KafkaConsumerEndpointConfiguration? endpointConfiguration) =
            consumers
                .OfType<KafkaConsumer>()
                .SelectMany(
                    consumer => consumer.Configuration.Endpoints.Select(
                        endpoint =>
                            (ConsumerConfiguration: consumer.Configuration, EndpointConfiguration: endpoint)))
                .FirstOrDefault(
                    tuple => tuple.EndpointConfiguration.RawName == endpointName ||
                             tuple.EndpointConfiguration.FriendlyName == endpointName);

        if (consumerConfiguration == null || endpointConfiguration == null)
            return null;

        return GetProducer(
            producer => producer
                .WithBootstrapServers(consumerConfiguration.BootstrapServers)
                .Produce<object>(endpoint => MapEndpoint(endpoint, endpointConfiguration))); // TODO: Map other settings (e.g. encryption)
    }

    // TODO: Test all cases
    private static void MapEndpoint(
        KafkaProducerEndpointConfigurationBuilder<object> endpoint,
        KafkaConsumerEndpointConfiguration endpointConfiguration)
    {
        endpoint.SerializeUsing(endpointConfiguration.Deserializer.GetCompatibleSerializer());
        endpoint.ProduceTo(endpointConfiguration.TopicPartitions.First().TopicPartition);
    }
}
