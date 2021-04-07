﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using Microsoft.Extensions.Hosting;
using Silverback.Messaging.BinaryFiles;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.Headers;
using Silverback.Messaging.Inbound;
using Silverback.Messaging.Inbound.ExactlyOnce;
using Silverback.Messaging.Inbound.Transaction;
using Silverback.Messaging.Outbound.Enrichers;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Sequences.Batch;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Messaging.Serialization;
using Silverback.Util;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Adds the <c>AddBroker</c> method to the <see cref="IBrokerOptionsBuilder" />.
    /// </summary>
    public static class BrokerOptionsBuilderAddBrokerExtensions
    {
        /// <summary>
        ///     Adds the specified <see cref="IBroker" /> implementation to allow producing and consuming messages.
        /// </summary>
        /// <typeparam name="TBroker">
        ///     The type of the <see cref="IBroker" /> implementation to add.
        /// </typeparam>
        /// <param name="brokerOptionsBuilder">
        ///     The <see cref="IBrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to
        ///     add the services to.
        /// </param>
        /// <returns>
        ///     The <see cref="IBrokerOptionsBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IBrokerOptionsBuilder AddBroker<TBroker>(
            this IBrokerOptionsBuilder brokerOptionsBuilder)
            where TBroker : class, IBroker
        {
            Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

            if (!brokerOptionsBuilder.SilverbackBuilder.Services.ContainsAny<IBroker>())
            {
                // Configuration IHostedService
                brokerOptionsBuilder.SilverbackBuilder.Services
                    .AddSingleton<IHostedService, BrokerConnectorService>()
                    .AddSingleton<EndpointsConfiguratorsInvoker>();

                // Pipeline
                brokerOptionsBuilder.SilverbackBuilder.Services
                    .AddTransient(typeof(IBrokerBehaviorsProvider<>), typeof(BrokerBehaviorsProvider<>));

                // Pipeline - Activity
                brokerOptionsBuilder.SilverbackBuilder
                    .AddSingletonBrokerBehavior<ActivityProducerBehavior>()
                    .AddSingletonBrokerBehavior<ActivityConsumerBehavior>();

                // Pipeline - Serialization
                brokerOptionsBuilder.SilverbackBuilder
                    .AddSingletonBrokerBehavior<SerializerProducerBehavior>()
                    .AddSingletonBrokerBehavior<DeserializerConsumerBehavior>();

                // Pipeline - Encryption
                brokerOptionsBuilder.SilverbackBuilder
                    .AddSingletonBrokerBehavior<EncryptorProducerBehavior>()
                    .AddSingletonBrokerBehavior<DecryptorConsumerBehavior>()
                    .Services
                    .AddSingleton<ISilverbackCryptoStreamFactory, SilverbackCryptoStreamFactory>();

                // Pipeline - Headers
                brokerOptionsBuilder.SilverbackBuilder
                    .AddSingletonBrokerBehavior<HeadersWriterProducerBehavior>()
                    .AddSingletonBrokerBehavior<HeadersReaderConsumerBehavior>()
                    .AddSingletonBrokerBehavior<CustomHeadersMapperProducerBehavior>()
                    .AddSingletonBrokerBehavior<CustomHeadersMapperConsumerBehavior>()
                    .Services
                    .AddSingleton<ICustomHeadersMappings>(new CustomHeadersMappings());

                // Pipeline - Sequences (Chunking, Batch, ...)
                brokerOptionsBuilder.SilverbackBuilder
                    .AddSingletonBrokerBehavior<SequencerProducerBehavior>()
                    .AddSingletonBrokerBehavior<SequencerConsumerBehavior>()
                    .AddSingletonBrokerBehavior<RawSequencerConsumerBehavior>()
                    .AddSingletonSequenceWriter<ChunkSequenceWriter>()
                    .AddSingletonSequenceReader<ChunkSequenceReader>()
                    .AddTransientSequenceReader<BatchSequenceReader>()
                    .Services
                    .AddTransient(typeof(ISequenceStore), typeof(DefaultSequenceStore));

                // Pipeline - Binary File
                brokerOptionsBuilder.SilverbackBuilder
                    .AddSingletonBrokerBehavior<BinaryFileHandlerProducerBehavior>()
                    .AddSingletonBrokerBehavior<BinaryFileHandlerConsumerBehavior>();

                // Pipeline - Producer basic logic
                brokerOptionsBuilder.SilverbackBuilder
                    .AddSingletonBrokerBehavior<MessageEnricherProducerBehavior>()
                    .AddSingletonBrokerBehavior<MessageIdInitializerProducerBehavior>()
                    .AddSingletonBrokerBehavior<EndpointNameResolverProducerBehavior>();

                // Pipeline - Consumer basic logic
                brokerOptionsBuilder.SilverbackBuilder
                    .AddSingletonBrokerBehavior<FatalExceptionLoggerConsumerBehavior>()
                    .AddSingletonBrokerBehavior<TransactionHandlerConsumerBehavior>()
                    .AddSingletonBrokerBehavior<ExactlyOnceGuardConsumerBehavior>()
                    .AddSingletonBrokerBehavior<PublisherConsumerBehavior>();
            }

            // Register the broker as IBroker and the type itself, both resolving to the same instance
            brokerOptionsBuilder.SilverbackBuilder.Services
                .AddSingleton<IBroker, TBroker>()
                .AddSingleton(
                    servicesProvider =>
                        servicesProvider.GetServices<IBroker>().OfType<TBroker>().FirstOrDefault());

            FindOptionsConfigurator<TBroker>()?.Configure(brokerOptionsBuilder);

            return brokerOptionsBuilder;
        }

        private static IBrokerOptionsConfigurator<TBroker>? FindOptionsConfigurator<TBroker>()
            where TBroker : IBroker
        {
            var type = typeof(TBroker).Assembly.GetTypes()
                .FirstOrDefault(t => typeof(IBrokerOptionsConfigurator<TBroker>).IsAssignableFrom(t));

            if (type == null)
                return null;

            return (IBrokerOptionsConfigurator<TBroker>)Activator.CreateInstance(type);
        }
    }
}
