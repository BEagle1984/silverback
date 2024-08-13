// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.BrokerMessageIdentifiersTracking;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Consuming.ContextEnrichment;
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
            .AddSingletonBrokerBehavior<KafkaContextEnricherConsumerBehavior>()
            .AddSingletonBrokerClientCallback<KafkaConsumerLocalTimeoutMonitor>()
            .AddExtensibleFactory<IKafkaOffsetStoreFactory, KafkaOffsetStoreFactory>()
            .Services
            .AddScoped<KafkaClientsConfigurationActions>()
            .AddTransient<IConfluentProducerBuilder, ConfluentProducerBuilder>()
            .AddTransient<IConfluentConsumerBuilder, ConfluentConsumerBuilder>()
            .AddSingleton<IConfluentAdminClientFactory, ConfluentAdminClientFactory>()
            .AddTransient<IBrokerClientsInitializer, KafkaProducersInitializer>()
            .AddTransient<IBrokerClientsInitializer, KafkaConsumersInitializer>()
            .AddTransient<KafkaProducersInitializer>()
            .AddSingleton<IConfluentProducerWrapperFactory, ConfluentProducerWrapperFactory>()
            .AddSingleton<IKafkaTransactionalProducerCollection, KafkaTransactionalProducerCollection>()
            .AddSingleton<IMovePolicyMessageEnricher<KafkaProducerEndpoint>, KafkaMovePolicyMessageEnricher>()
            .AddSingleton<IMovePolicyMessageEnricher<KafkaConsumerEndpoint>, KafkaMovePolicyMessageEnricher>()
            .AddTransient<KafkaOffsetStoreScope>(services => services.GetRequiredService<SilverbackContext>().GetKafkaOffsetStoreScope());

        AddChunkEnricher(brokerOptionsBuilder);
        AddBrokerLogEnrichers(brokerOptionsBuilder);
        AddActivityEnrichers(brokerOptionsBuilder);
        AddOffsetsTracker(brokerOptionsBuilder);

        return brokerOptionsBuilder;
    }

    private static void AddChunkEnricher(BrokerOptionsBuilder brokerOptionsBuilder)
    {
        ChunkEnricherFactory factory = brokerOptionsBuilder.SilverbackBuilder.Services.GetSingletonServiceInstance<ChunkEnricherFactory>() ??
                                       throw new InvalidOperationException("ChunkEnricherFactory not found, WithConnectionToMessageBroker has not been called.");

        if (!factory.HasFactory<KafkaProducerEndpoint>())
            factory.AddFactory<KafkaProducerEndpoint>(_ => new KafkaChunkEnricher());
    }

    private static void AddBrokerLogEnrichers(BrokerOptionsBuilder brokerOptionsBuilder)
    {
        BrokerLogEnricherFactory factory = brokerOptionsBuilder.SilverbackBuilder.Services.GetSingletonServiceInstance<BrokerLogEnricherFactory>() ??
                                           throw new InvalidOperationException("BrokerLogEnricherFactory not found, WithConnectionToMessageBroker has not been called.");

        if (!factory.HasFactory<KafkaProducerEndpointConfiguration>())
            factory.AddFactory<KafkaProducerEndpointConfiguration>(_ => new KafkaLogEnricher());
        if (!factory.HasFactory<KafkaConsumerEndpointConfiguration>())
            factory.AddFactory<KafkaConsumerEndpointConfiguration>(_ => new KafkaLogEnricher());
    }

    private static void AddActivityEnrichers(BrokerOptionsBuilder brokerOptionsBuilder)
    {
        ActivityEnricherFactory factory = brokerOptionsBuilder.SilverbackBuilder.Services.GetSingletonServiceInstance<ActivityEnricherFactory>() ??
                                          throw new InvalidOperationException("ActivityEnricherFactory not found, WithConnectionToMessageBroker has not been called.");

        if (!factory.HasFactory<KafkaProducerEndpointConfiguration>())
            factory.AddFactory<KafkaProducerEndpointConfiguration>(_ => new KafkaActivityEnricher());
        if (!factory.HasFactory<KafkaConsumerEndpointConfiguration>())
            factory.AddFactory<KafkaConsumerEndpointConfiguration>(_ => new KafkaActivityEnricher());
    }

    private static void AddOffsetsTracker(BrokerOptionsBuilder brokerOptionsBuilder)
    {
        BrokerMessageIdentifiersTrackerFactory factory = brokerOptionsBuilder.SilverbackBuilder.Services.GetSingletonServiceInstance<BrokerMessageIdentifiersTrackerFactory>() ??
                                                         throw new InvalidOperationException("BrokerMessageIdentifierTrackerFactory not found, WithConnectionToMessageBroker has not been called.");

        if (!factory.HasFactory<KafkaProducerEndpointConfiguration>())
            factory.AddFactory<KafkaProducerEndpointConfiguration>(_ => new OffsetsTracker());
        if (!factory.HasFactory<KafkaConsumerEndpointConfiguration>())
            factory.AddFactory<KafkaConsumerEndpointConfiguration>(_ => new OffsetsTracker());
    }
}
