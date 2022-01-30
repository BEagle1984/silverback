// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.Outbound;
using Silverback.Messaging.Outbound.Enrichers;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     This class will be located via assembly scanning and invoked when a <see cref="KafkaBroker" /> is added to the
///     <see cref="IServiceCollection" />.
/// </summary>
public class KafkaBrokerOptionsConfigurator : IBrokerOptionsConfigurator<KafkaBroker>
{
    /// <inheritdoc cref="IBrokerOptionsConfigurator{TBroker}.Configure" />
    public void Configure(BrokerOptionsBuilder brokerOptionsBuilder)
    {
        Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

        brokerOptionsBuilder.SilverbackBuilder
            .AddSingletonBrokerBehavior<KafkaMessageKeyInitializerProducerBehavior>()
            .AddSingletonBrokerCallbackHandler<KafkaConsumerLocalTimeoutMonitor>()
            .Services
            .AddTransient<IConfluentProducerBuilder, ConfluentProducerBuilder>()
            .AddTransient<IConfluentConsumerBuilder, ConfluentConsumerBuilder>()
            .AddTransient<IConfluentAdminClientBuilder, ConfluentAdminClientBuilder>()
            .AddSingleton<IConfluentProducersCache, ConfluentProducersCache>()
            .AddSingleton<IBrokerLogEnricher<KafkaProducerConfiguration>, KafkaLogEnricher>()
            .AddSingleton<IBrokerLogEnricher<KafkaConsumerConfiguration>, KafkaLogEnricher>()
            .AddSingleton<IBrokerActivityEnricher<KafkaProducerConfiguration>, KafkaActivityEnricher>()
            .AddSingleton<IBrokerActivityEnricher<KafkaConsumerConfiguration>, KafkaActivityEnricher>()
            .AddSingleton<IMovePolicyMessageEnricher<KafkaProducerEndpoint>, KafkaMovePolicyMessageEnricher>()
            .AddSingleton<IMovePolicyMessageEnricher<KafkaConsumerEndpoint>, KafkaMovePolicyMessageEnricher>();

        ChunkEnricherFactory? chunkEnricherFactory =
            brokerOptionsBuilder.SilverbackBuilder.Services.GetSingletonServiceInstance<ChunkEnricherFactory>();

        if (chunkEnricherFactory == null)
            throw new InvalidOperationException("ChunkEnricherFactory not found, AddBroker has not been called.");

        if (!chunkEnricherFactory.HasFactory<KafkaProducerEndpoint>())
            chunkEnricherFactory.AddFactory<KafkaProducerEndpoint>(() => new KafkaChunkEnricher());

    }
}
