// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using Microsoft.Extensions.Hosting;
using Silverback.Messaging.BinaryFiles;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.Headers;
using Silverback.Messaging.LargeMessages;
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
        public static IBrokerOptionsBuilder AddBroker<TBroker>(this IBrokerOptionsBuilder brokerOptionsBuilder)
            where TBroker : class, IBroker
        {
            Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

            if (!brokerOptionsBuilder.SilverbackBuilder.Services.ContainsAny<IBroker>())
            {
                // Configuration IHostedService
                brokerOptionsBuilder.SilverbackBuilder.Services
                    .AddSingleton<IHostedService, BrokerConnectorService>()
                    .AddSingleton<EndpointsConfiguratorsInvoker>();

                // Pipeline - Activity
                brokerOptionsBuilder.SilverbackBuilder
                    .AddSingletonBrokerBehavior<ActivityProducerBehavior>()
                    .AddSingletonBrokerBehavior<ActivityConsumerBehavior>();

                // Pipeline - Exception Logger
                brokerOptionsBuilder.SilverbackBuilder
                    .AddSingletonBrokerBehavior<FatalExceptionLoggerConsumerBehavior>();

                // Pipeline - Serialization
                brokerOptionsBuilder.SilverbackBuilder
                    .AddSingletonBrokerBehavior<SerializerProducerBehavior>()
                    .AddSingletonBrokerBehavior<DeserializerConsumerBehavior>();

                // Pipeline - Encryption
                brokerOptionsBuilder.SilverbackBuilder
                    .AddSingletonBrokerBehavior<EncryptorProducerBehavior>()
                    .AddSingletonBrokerBehavior<DecryptorConsumerBehavior>();

                // Pipeline - Headers
                brokerOptionsBuilder.SilverbackBuilder
                    .AddSingletonBrokerBehavior<HeadersWriterProducerBehavior>()
                    .AddSingletonBrokerBehavior<HeadersReaderConsumerBehavior>()
                    .Services
                    .AddSingleton<IMessageTransformerFactory, MessageTransformerFactory>();
                brokerOptionsBuilder.SilverbackBuilder
                    .AddSingletonBrokerBehavior<CustomHeadersMapperProducerBehavior>()
                    .AddSingletonBrokerBehavior<CustomHeadersMapperConsumerBehavior>()
                    .Services
                    .AddSingleton<ICustomHeadersMappings>(new CustomHeadersMappings());

                // Pipeline - Chunking
                brokerOptionsBuilder.SilverbackBuilder
                    .AddSingletonBrokerBehavior<ChunkSplitterProducerBehavior>()
                    .AddSingletonBrokerBehavior(
                        serviceProvider =>
                            serviceProvider.GetRequiredService<ChunkAggregatorConsumerBehavior>())
                    .AddSingletonSubscriber<ChunkAggregatorConsumerBehavior>()
                    .Services
                    .AddScoped<ChunkAggregator>();

                // Pipeline - Binary File
                brokerOptionsBuilder.SilverbackBuilder
                    .AddSingletonBrokerBehavior<BinaryFileHandlerProducerBehavior>()
                    .AddSingletonBrokerBehavior<BinaryFileHandlerConsumerBehavior>();

                // Pipeline - Message Id Initializer
                brokerOptionsBuilder.SilverbackBuilder
                    .AddSingletonBrokerBehavior<MessageIdInitializerProducerBehavior>();

                // Pipeline - Inbound Processor
                brokerOptionsBuilder.SilverbackBuilder
                    .AddSingletonBrokerBehavior<InboundProcessorConsumerBehaviorFactory>()
                    .AddScopedSubscriber<ConsumerTransactionManager>()
                    .Services
                    .AddSingleton<IErrorPolicyBuilder, ErrorPolicyBuilder>();

                // Support - Transactional Lists
                brokerOptionsBuilder.SilverbackBuilder.Services
                    .AddSingleton(typeof(TransactionalListSharedItems<>))
                    .AddSingleton(typeof(TransactionalDictionarySharedItems<,>));
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
