// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using Silverback.Messaging.BinaryFiles;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.Headers;
using Silverback.Messaging.LargeMessages;
using Silverback.Messaging.Serialization;
using Silverback.Util;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Adds the <c>
    ///         AddBroker
    ///     </c> method to the <see cref="IBrokerOptionsBuilder" />.
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
                // Pipeline - Serialization
                brokerOptionsBuilder.SilverbackBuilder.Services
                    .AddSingletonBrokerBehavior<SerializerProducerBehavior>()
                    .AddSingletonBrokerBehavior<DeserializerConsumerBehavior>();

                // Pipeline - Encryption
                brokerOptionsBuilder.SilverbackBuilder.Services
                    .AddSingletonBrokerBehavior<EncryptorProducerBehavior>()
                    .AddSingletonBrokerBehavior<DecryptorConsumerBehavior>();

                // Pipeline - Headers
                brokerOptionsBuilder.SilverbackBuilder.Services
                    .AddSingletonBrokerBehavior<HeadersWriterProducerBehavior>()
                    .AddSingletonBrokerBehavior<HeadersReaderConsumerBehavior>()
                    .AddSingleton<IMessageTransformerFactory, MessageTransformerFactory>();

                // Pipeline - Chunking
                brokerOptionsBuilder.SilverbackBuilder.Services
                    .AddSingletonBrokerBehavior<ChunkSplitterProducerBehavior>()
                    .AddSingletonBrokerBehavior(
                        serviceProvider =>
                            serviceProvider.GetRequiredService<ChunkAggregatorConsumerBehavior>())
                    .AddSingletonSubscriber<ChunkAggregatorConsumerBehavior>()
                    .AddScoped<ChunkAggregator>();

                // Pipeline - Binary File
                brokerOptionsBuilder.SilverbackBuilder.Services
                    .AddSingletonBrokerBehavior<BinaryFileHandlerProducerBehavior>()
                    .AddSingletonBrokerBehavior<BinaryFileHandlerConsumerBehavior>();

                // Pipeline - Message Id Initializer
                brokerOptionsBuilder.SilverbackBuilder.Services
                    .AddSingletonBrokerBehavior<MessageIdInitializerProducerBehavior>();

                // Pipeline - Inbound Processor
                brokerOptionsBuilder.SilverbackBuilder.Services
                    .AddSingletonBrokerBehavior<InboundProcessorConsumerBehaviorFactory>()
                    .AddScopedSubscriber<ConsumerTransactionManager>();

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
