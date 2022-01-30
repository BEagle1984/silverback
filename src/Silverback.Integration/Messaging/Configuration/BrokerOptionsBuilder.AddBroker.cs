﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.BinaryMessages;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.Headers;
using Silverback.Messaging.Inbound;
using Silverback.Messaging.Inbound.Transaction;
using Silverback.Messaging.Outbound.Enrichers;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Sequences.Batch;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Messaging.Serialization;
using Silverback.Messaging.Validation;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <content>
///     Adds the AddBroker method to the <see cref="BrokerOptionsBuilder" />.
/// </content>
public sealed partial class BrokerOptionsBuilder
{
    /// <summary>
    ///     Adds the specified <see cref="IBroker" /> implementation to allow producing and consuming messages.
    /// </summary>
    /// <typeparam name="TBroker">
    ///     The type of the <see cref="IBroker" /> implementation to add.
    /// </typeparam>
    /// <returns>
    ///     The <see cref="BrokerOptionsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public BrokerOptionsBuilder AddBroker<TBroker>()
        where TBroker : class, IBroker
    {
        if (!SilverbackBuilder.Services.ContainsAny<IBroker>())
        {
            // Pipeline
            SilverbackBuilder.Services
                .AddTransient(typeof(IBrokerBehaviorsProvider<>), typeof(BrokerBehaviorsProvider<>));

            // Pipeline - Activity
            SilverbackBuilder
                .AddSingletonBrokerBehavior<ActivityProducerBehavior>()
                .AddSingletonBrokerBehavior<ActivityConsumerBehavior>();

            // Pipeline - Validation
            SilverbackBuilder
                .AddSingletonBrokerBehavior<ValidatorProducerBehavior>()
                .AddSingletonBrokerBehavior<ValidatorConsumerBehavior>();

            // Pipeline - Serialization
            SilverbackBuilder
                .AddSingletonBrokerBehavior<SerializerProducerBehavior>()
                .AddSingletonBrokerBehavior<DeserializerConsumerBehavior>();

            // Pipeline - Encryption
            SilverbackBuilder
                .AddSingletonBrokerBehavior<EncryptorProducerBehavior>()
                .AddSingletonBrokerBehavior<DecryptorConsumerBehavior>()
                .Services
                .AddSingleton<ISilverbackCryptoStreamFactory, SilverbackCryptoStreamFactory>();

            // Pipeline - Headers
            SilverbackBuilder
                .AddSingletonBrokerBehavior<HeadersWriterProducerBehavior>()
                .AddSingletonBrokerBehavior<HeadersReaderConsumerBehavior>()
                .AddSingletonBrokerBehavior<CustomHeadersMapperProducerBehavior>()
                .AddSingletonBrokerBehavior<CustomHeadersMapperConsumerBehavior>()
                .Services
                .AddSingleton<ICustomHeadersMappings>(new CustomHeadersMappings());

            // Pipeline - Sequences (chunking, batch, ...)
            SilverbackBuilder
                .AddSingletonBrokerBehavior<SequencerProducerBehavior>()
                .AddSingletonBrokerBehavior<SequencerConsumerBehavior>()
                .AddSingletonBrokerBehavior<RawSequencerConsumerBehavior>()
                .AddSingletonSequenceWriter<ChunkSequenceWriter>()
                .AddSingletonSequenceReader<ChunkSequenceReader>()
                .AddTransientSequenceReader<BatchSequenceReader>()
                .AddTypeBasedExtensibleFactory<IChunkEnricherFactory, ChunkEnricherFactory>()
                .Services
                .AddTransient(typeof(ISequenceStore), typeof(DefaultSequenceStore));

            // Pipeline - Binary message
            SilverbackBuilder
                .AddSingletonBrokerBehavior<BinaryMessageHandlerProducerBehavior>()
                .AddSingletonBrokerBehavior<BinaryMessageHandlerConsumerBehavior>();

            // Pipeline - Producer basic logic
            SilverbackBuilder
                .AddSingletonBrokerBehavior<MessageEnricherProducerBehavior>()
                .AddSingletonBrokerBehavior<MessageIdInitializerProducerBehavior>();

            // Pipeline - Consumer basic logic
            SilverbackBuilder
                .AddSingletonBrokerBehavior<FatalExceptionLoggerConsumerBehavior>()
                .AddSingletonBrokerBehavior<TransactionHandlerConsumerBehavior>()
                .AddSingletonBrokerBehavior<PublisherConsumerBehavior>();
        }

        // Register the broker as IBroker and the type itself, both resolving to the same instance
        SilverbackBuilder.Services
            .AddSingleton<IBroker, TBroker>()
            .AddSingleton(
                servicesProvider =>
                    servicesProvider.GetServices<IBroker>().OfType<TBroker>().FirstOrDefault());

        FindOptionsConfigurator<TBroker>()?.Configure(this);

        return this;
    }

    private static IBrokerOptionsConfigurator<TBroker>? FindOptionsConfigurator<TBroker>()
        where TBroker : IBroker
    {
        Type? type = typeof(TBroker).Assembly.GetTypes()
            .FirstOrDefault(t => typeof(IBrokerOptionsConfigurator<TBroker>).IsAssignableFrom(t));

        if (type == null)
            return null;

        return (IBrokerOptionsConfigurator<TBroker>)Activator.CreateInstance(type)!;
    }
}