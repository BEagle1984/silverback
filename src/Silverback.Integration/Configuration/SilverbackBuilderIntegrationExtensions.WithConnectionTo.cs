// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.BinaryMessages;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Consuming;
using Silverback.Messaging.Consuming.Transaction;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.Headers;
using Silverback.Messaging.Producing.Enrichers;
using Silverback.Messaging.Producing.Routing;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Sequences.Batch;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Messaging.Serialization;
using Silverback.Messaging.Validation;
using Silverback.Util;

namespace Silverback.Configuration;

/// <content>
///     Adds the WithConnectionToMessageBroker method to the <see cref="SilverbackBuilder" />.
/// </content>
public static partial class SilverbackBuilderIntegrationExtensions
{
    /// <summary>
    ///     Registers the types needed to connect with a message broker.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <param name="optionsAction">
    ///     Additional options such as the actual message brokers to be used.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder WithConnectionToMessageBroker(
        this SilverbackBuilder builder,
        Action<BrokerOptionsBuilder>? optionsAction = null)
    {
        Check.NotNull(builder, nameof(builder));

        AddClientsManagement(builder);
        AddOutboundRouting(builder);
        AddLoggers(builder);
        AddEnrichers(builder);
        AddBrokerBehaviors(builder);

        builder.Services.AddSingleton<IBrokerClientCallbacksInvoker, BrokerClientCallbackInvoker>();
        builder.EnableStorage();

        BrokerOptionsBuilder optionsBuilder = new(builder);
        optionsAction?.Invoke(optionsBuilder);

        return builder;
    }

    private static void AddClientsManagement(SilverbackBuilder builder) =>
        builder.Services
            .AddSingleton<BrokerClientCollection>()
            .AddSingleton<ProducerCollection>()
            .AddSingleton<IProducerCollection>(serviceProvider => serviceProvider.GetService<ProducerCollection>())
            .AddSingleton<ConsumerCollection>()
            .AddSingleton<IConsumerCollection>(serviceProvider => serviceProvider.GetService<ConsumerCollection>())
            .AddHostedService<BrokerClientsConnectorService>()
            .AddTransient<IBrokerClientsConnector, BrokerClientsConnector>()
            .AddTransient<BrokerClientsBootstrapper>()
            .AddSingleton(new BrokerClientConnectionOptions());

    private static void AddOutboundRouting(SilverbackBuilder builder) =>
        builder
            .AddScopedBehavior<OutboundRouterBehavior>()
            .AddScopedBehavior<ProduceBehavior>()
            .AddExtensibleFactory<IOutboxReaderFactory, OutboxReaderFactory>()
            .AddExtensibleFactory<IOutboxWriterFactory, OutboxWriterFactory>()
            .Services
            .AddSingleton<IOutboundEnvelopeFactory, OutboundEnvelopeFactory>()
            .AddSingleton<IOutboundRoutingConfiguration>(new OutboundRoutingConfiguration());

    private static void AddLoggers(SilverbackBuilder builder) =>
        builder
            .Services
            .AddSingleton(typeof(IConsumerLogger<>), typeof(ConsumerLogger<>))
            .AddSingleton(typeof(IProducerLogger<>), typeof(ProducerLogger<>))
            .AddSingleton<InternalConsumerLoggerFactory>()
            .AddSingleton<InternalProducerLoggerFactory>();

    private static void AddEnrichers(SilverbackBuilder builder) =>
        builder
            .AddTypeBasedExtensibleFactory<IBrokerLogEnricherFactory, BrokerLogEnricherFactory>()
            .AddTypeBasedExtensibleFactory<IActivityEnricherFactory, ActivityEnricherFactory>()
            .Services
            .AddSingleton<IBrokerOutboundMessageEnrichersFactory, BrokerOutboundMessageEnrichersFactory>();

    private static void AddBrokerBehaviors(SilverbackBuilder builder)
    {
        builder.Services
            .AddTransient(typeof(IBrokerBehaviorsProvider<>), typeof(BrokerBehaviorsProvider<>));

        // Producer basic logic
        builder
            .AddSingletonBrokerBehavior<MessageEnricherProducerBehavior>()
            .AddSingletonBrokerBehavior<MessageIdInitializerProducerBehavior>();

        // Consumer basic logic
        builder
            .AddSingletonBrokerBehavior<FatalExceptionLoggerConsumerBehavior>()
            .AddSingletonBrokerBehavior<TransactionHandlerConsumerBehavior>()
            .AddSingletonBrokerBehavior<PublisherConsumerBehavior>();

        // Activity
        builder
            .AddSingletonBrokerBehavior<ActivityProducerBehavior>()
            .AddSingletonBrokerBehavior<ActivityConsumerBehavior>();

        // Validation
        builder
            .AddSingletonBrokerBehavior<ValidatorProducerBehavior>()
            .AddSingletonBrokerBehavior<ValidatorConsumerBehavior>();

        // Serialization
        builder
            .AddSingletonBrokerBehavior<SerializerProducerBehavior>()
            .AddSingletonBrokerBehavior<DeserializerConsumerBehavior>();

        // Encryption
        builder
            .AddSingletonBrokerBehavior<EncryptorProducerBehavior>()
            .AddSingletonBrokerBehavior<DecryptorConsumerBehavior>()
            .Services
            .AddSingleton<ISilverbackCryptoStreamFactory, SilverbackCryptoStreamFactory>();

        // Headers
        builder
            .AddSingletonBrokerBehavior<HeadersWriterProducerBehavior>()
            .AddSingletonBrokerBehavior<HeadersReaderConsumerBehavior>()
            .AddSingletonBrokerBehavior<CustomHeadersMapperProducerBehavior>()
            .AddSingletonBrokerBehavior<CustomHeadersMapperConsumerBehavior>()
            .Services
            .AddSingleton<ICustomHeadersMappings>(new CustomHeadersMappings());

        // Sequences (chunking, batch, ...)
        builder
            .AddSingletonBrokerBehavior<SequencerProducerBehavior>()
            .AddSingletonBrokerBehavior<SequencerConsumerBehavior>()
            .AddSingletonBrokerBehavior<RawSequencerConsumerBehavior>()
            .AddSingletonSequenceWriter<ChunkSequenceWriter>()
            .AddSingletonSequenceReader<ChunkSequenceReader>()
            .AddTransientSequenceReader<BatchSequenceReader>()
            .AddTypeBasedExtensibleFactory<IChunkEnricherFactory, ChunkEnricherFactory>()
            .Services
            .AddTransient(typeof(ISequenceStore), typeof(DefaultSequenceStore));

        // Binary message
        builder
            .AddSingletonBrokerBehavior<BinaryMessageHandlerProducerBehavior>()
            .AddSingletonBrokerBehavior<BinaryMessageHandlerConsumerBehavior>();
    }
}
