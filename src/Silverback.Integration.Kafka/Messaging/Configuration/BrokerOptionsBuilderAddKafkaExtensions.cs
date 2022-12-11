// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.Producing;
using Silverback.Messaging.Producing.Enrichers;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Adds the <c>AddKafka</c> method to the <see cref="BrokerOptionsBuilder" />.
/// </summary>
public static class BrokerOptionsBuilderAddKafkaExtensions
{
    /// <summary>
    ///     Registers Apache Kafka as message broker.
    /// </summary>
    /// <param name="brokerOptionsBuilder">
    ///     The <see cref="BrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <returns>
    ///     The <see cref="BrokerOptionsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static BrokerOptionsBuilder AddKafka(this BrokerOptionsBuilder brokerOptionsBuilder)
    {
        Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

        if (brokerOptionsBuilder.SilverbackBuilder.Services.ContainsAny<KafkaClientsConfigurationActions>())
            return brokerOptionsBuilder;

        brokerOptionsBuilder.SilverbackBuilder
            .AddSingletonBrokerBehavior<KafkaMessageKeyInitializerProducerBehavior>()
            .AddSingletonBrokerBehavior<KafkaOffsetStoreConsumerBehavior>()
            .AddSingletonBrokerClientCallback<KafkaConsumerLocalTimeoutMonitor>()
            .AddExtensibleFactory<IKafkaOffsetStoreFactory, KafkaOffsetStoreFactory>()
            .Services
            .AddScoped<KafkaClientsConfigurationActions>()
            .AddTransient<IConfluentProducerBuilder, ConfluentProducerBuilder>()
            .AddTransient<IConfluentConsumerBuilder, ConfluentConsumerBuilder>()
            .AddTransient<IConfluentAdminClientBuilder, ConfluentAdminClientBuilder>()
            .AddTransient<IBrokerClientsInitializer, KafkaProducersInitializer>()
            .AddTransient<IBrokerClientsInitializer, KafkaConsumersInitializer>()
            .AddTransient<KafkaProducersInitializer>()
            .AddSingleton<IBrokerActivityEnricher<KafkaProducerEndpointConfiguration>, KafkaActivityEnricher>()
            .AddSingleton<IBrokerActivityEnricher<KafkaConsumerEndpointConfiguration>, KafkaActivityEnricher>()
            .AddSingleton<IMovePolicyMessageEnricher<KafkaProducerEndpoint>, KafkaMovePolicyMessageEnricher>()
            .AddSingleton<IMovePolicyMessageEnricher<KafkaConsumerEndpoint>, KafkaMovePolicyMessageEnricher>()
            .AddTransient<KafkaOffsetStoreScope>(
                services =>
                {
                    if (!services.GetRequiredService<SilverbackContext>().TryGetKafkaOffsetStoreScope(out KafkaOffsetStoreScope? scope))
                        throw new InvalidOperationException("No kafka offset store scope."); // TODO: review message

                    return scope;
                });

        AddChunkEnricher(brokerOptionsBuilder);
        AddBrokerLogEnrichers(brokerOptionsBuilder);

        return brokerOptionsBuilder;
    }

    private static void AddChunkEnricher(BrokerOptionsBuilder brokerOptionsBuilder)
    {
        ChunkEnricherFactory? chunkEnricherFactory = brokerOptionsBuilder.SilverbackBuilder.Services.GetSingletonServiceInstance<ChunkEnricherFactory>();

        if (chunkEnricherFactory == null)
            throw new InvalidOperationException("ChunkEnricherFactory not found, WithConnectionToMessageBroker has not been called.");

        if (!chunkEnricherFactory.HasFactory<KafkaProducerEndpoint>())
            chunkEnricherFactory.AddFactory<KafkaProducerEndpoint>(() => new KafkaChunkEnricher());
    }

    private static void AddBrokerLogEnrichers(BrokerOptionsBuilder brokerOptionsBuilder)
    {
        BrokerLogEnricherFactory? logEnricherFactory = brokerOptionsBuilder.SilverbackBuilder.Services.GetSingletonServiceInstance<BrokerLogEnricherFactory>();

        if (logEnricherFactory == null)
            throw new InvalidOperationException("ConsumerLogEnricherFactory not found, WithConnectionToMessageBroker has not been called.");

        if (!logEnricherFactory.HasFactory<KafkaProducerEndpointConfiguration>())
            logEnricherFactory.AddFactory<KafkaProducerEndpointConfiguration>(() => new KafkaLogEnricher());
        if (!logEnricherFactory.HasFactory<KafkaConsumerEndpointConfiguration>())
            logEnricherFactory.AddFactory<KafkaConsumerEndpointConfiguration>(() => new KafkaLogEnricher());
    }
}
