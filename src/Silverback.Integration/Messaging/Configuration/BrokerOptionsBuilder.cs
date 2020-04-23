// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Silverback.Background;
using Silverback.Messaging.BinaryFiles;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Connectors.Behaviors;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Headers;
using Silverback.Messaging.LargeMessages;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    /// <inheritdoc cref="IBrokerOptionsBuilder" />
    [SuppressMessage("ReSharper", "UnusedMethodReturnValue.Local")]
    public class BrokerOptionsBuilder : IBrokerOptionsBuilder
    {
        public BrokerOptionsBuilder(ISilverbackBuilder silverbackBuilder)
        {
            SilverbackBuilder = silverbackBuilder;
        }

        public ISilverbackBuilder SilverbackBuilder { get; }

        #region AddBroker

        public IBrokerOptionsBuilder AddBroker<TBroker>()
            where TBroker : class, IBroker
        {
            if (!SilverbackBuilder.Services.ContainsAny<IBroker>())
            {
                SilverbackBuilder.Services
                    // Pipeline - Serialization
                    .AddSingletonBrokerBehavior<SerializerProducerBehavior>()
                    .AddSingletonBrokerBehavior<DeserializerConsumerBehavior>()
                    // Pipeline - Encryption
                    .AddSingletonBrokerBehavior<EncryptorProducerBehavior>()
                    .AddSingletonBrokerBehavior<DecryptorConsumerBehavior>()
                    // Pipeline - Headers
                    .AddSingletonBrokerBehavior<HeadersWriterProducerBehavior>()
                    .AddSingletonBrokerBehavior<HeadersReaderConsumerBehavior>()
                    .AddSingleton<IMessageTransformerFactory, MessageTransformerFactory>()
                    // Pipeline - Chunking
                    .AddSingletonBrokerBehavior<ChunkSplitterProducerBehavior>()
                    .AddSingletonBrokerBehavior(serviceProvider =>
                        serviceProvider.GetRequiredService<ChunkAggregatorConsumerBehavior>())
                    .AddSingletonSubscriber<ChunkAggregatorConsumerBehavior>()
                    .AddScoped<ChunkAggregator>()
                    // Pipeline - Binary File
                    .AddSingletonBrokerBehavior<BinaryFileHandlerProducerBehavior>()
                    .AddSingletonBrokerBehavior<BinaryFileHandlerConsumerBehavior>()
                    // Pipeline - Message Id Initializer
                    .AddSingletonBrokerBehavior<MessageIdInitializerProducerBehavior>()
                    // Pipeline - Inbound Processor
                    .AddSingletonBrokerBehavior<InboundProcessorConsumerBehaviorFactory>()
                    .AddScopedSubscriber<ConsumerTransactionManager>()
                    // Support - Transactional Lists 
                    .AddSingleton(typeof(TransactionalListSharedItems<>))
                    .AddSingleton(typeof(TransactionalDictionarySharedItems<,>));

#pragma warning disable 618
                // Deprecated
                AddMessageIdProvider<DefaultPropertiesMessageIdProvider>();
#pragma warning restore 618
            }

            SilverbackBuilder.Services
                .AddSingleton<IBroker, TBroker>()
                // ReSharper disable once RedundantTypeArgumentsOfMethod
                .AddSingleton<TBroker>(servicesProvider =>
                    servicesProvider.GetServices<IBroker>().OfType<TBroker>().FirstOrDefault());

            FindOptionsConfigurator<TBroker>()?.Configure(this);

            return this;
        }

        private static IBrokerOptionsConfigurator<TBroker> FindOptionsConfigurator<TBroker>()
            where TBroker : IBroker
        {
            var type = typeof(TBroker).Assembly.GetTypes()
                .FirstOrDefault(t => typeof(IBrokerOptionsConfigurator<TBroker>).IsAssignableFrom(t));

            if (type == null)
                return null;

            return (IBrokerOptionsConfigurator<TBroker>) Activator.CreateInstance(type);
        }

        #endregion

        #region AddInboundConnector

        public IBrokerOptionsBuilder AddInboundConnector<TConnector>()
            where TConnector : class, IInboundConnector
        {
            if (!SilverbackBuilder.Services.ContainsAny<IInboundConnector>())
            {
                SilverbackBuilder.Services.AddSingleton<ErrorPolicyHelper>();
            }

            SilverbackBuilder.Services.AddSingleton<IInboundConnector, TConnector>();

            return this;
        }

        public IBrokerOptionsBuilder AddInboundConnector() => AddInboundConnector<InboundConnector>();

        public IBrokerOptionsBuilder AddLoggedInboundConnector()
        {
            AddInboundConnector<LoggedInboundConnector>();

            return this;
        }

        public IBrokerOptionsBuilder AddLoggedInboundConnector<TLog>()
            where TLog : class, IInboundLog
        {
            AddLoggedInboundConnector();
            SilverbackBuilder.Services.AddScoped<IInboundLog, TLog>();

            return this;
        }

        public IBrokerOptionsBuilder AddOffsetStoredInboundConnector()
        {
            AddInboundConnector<OffsetStoredInboundConnector>();

            return this;
        }

        public IBrokerOptionsBuilder AddOffsetStoredInboundConnector<TStore>()
            where TStore : class, IOffsetStore
        {
            AddOffsetStoredInboundConnector();
            SilverbackBuilder.Services.AddScoped<IOffsetStore, TStore>();

            return this;
        }

        public IBrokerOptionsBuilder AddDbLoggedInboundConnector()
        {
            AddLoggedInboundConnector<DbInboundLog>();

            return this;
        }

        public IBrokerOptionsBuilder AddDbOffsetStoredInboundConnector()
        {
            AddOffsetStoredInboundConnector<DbOffsetStore>();

            return this;
        }

        #endregion

        #region AddOutboundConnector

        public IBrokerOptionsBuilder AddOutboundConnector<TConnector>()
            where TConnector : class, IOutboundConnector
        {
            if (!SilverbackBuilder.Services.ContainsAny<IOutboundConnector>())
            {
                SilverbackBuilder.Services.AddSingleton<IOutboundRoutingConfiguration, OutboundRoutingConfiguration>();
                SilverbackBuilder
                    .AddScopedBehavior<OutboundRouterBehavior>()
                    .AddScopedBehavior<OutboundProducerBehavior>();
            }

            SilverbackBuilder.Services.AddScoped<IOutboundConnector, TConnector>();

            return this;
        }

        public IBrokerOptionsBuilder AddOutboundConnector() => AddOutboundConnector<OutboundConnector>();

        public IBrokerOptionsBuilder AddDeferredOutboundConnector()
        {
            AddOutboundConnector<DeferredOutboundConnector>();
            SilverbackBuilder.AddScopedSubscriber<DeferredOutboundConnectorTransactionManager>();

            return this;
        }

        public IBrokerOptionsBuilder AddDeferredOutboundConnector<TQueueProducer>()
            where TQueueProducer : class, IOutboundQueueProducer
        {
            AddDeferredOutboundConnector();
            SilverbackBuilder.Services.AddScoped<IOutboundQueueProducer, TQueueProducer>();

            return this;
        }

        public IBrokerOptionsBuilder AddDbOutboundConnector()
        {
            AddDeferredOutboundConnector<DbOutboundQueueProducer>();

            return this;
        }

        #endregion

        #region AddOutboundWorker

        public IBrokerOptionsBuilder AddOutboundWorker(
            TimeSpan? interval = null,
            bool enforceMessageOrder = true,
            int readPackageSize = 100,
            DistributedLockSettings distributedLockSettings = null)
        {
            distributedLockSettings ??= new DistributedLockSettings();
            distributedLockSettings.ResourceName ??= "OutboundQueueWorker";

            SilverbackBuilder.Services
                .AddSingleton<IOutboundQueueWorker>(serviceProvider => new OutboundQueueWorker(
                    serviceProvider.GetRequiredService<IServiceScopeFactory>(),
                    serviceProvider.GetRequiredService<IBrokerCollection>(),
                    serviceProvider.GetRequiredService<ILogger<OutboundQueueWorker>>(),
                    serviceProvider.GetRequiredService<MessageLogger>(),
                    enforceMessageOrder, readPackageSize))
                .AddSingleton<IHostedService>(serviceProvider => new OutboundQueueWorkerService(
                    interval ?? TimeSpan.FromMilliseconds(500),
                    serviceProvider.GetRequiredService<IOutboundQueueWorker>(),
                    distributedLockSettings,
                    serviceProvider.GetService<IDistributedLockManager>() ?? new NullLockManager(),
                    serviceProvider.GetRequiredService<ILogger<OutboundQueueWorkerService>>()));

            return this;
        }

        public IBrokerOptionsBuilder AddOutboundWorker<TQueueConsumer>(
            TimeSpan? interval = null,
            bool enforceMessageOrder = true,
            int readPackageSize = 100,
            DistributedLockSettings distributedLockSettings = null)
            where TQueueConsumer : class, IOutboundQueueConsumer
        {
            AddOutboundWorker(
                interval,
                enforceMessageOrder,
                readPackageSize,
                distributedLockSettings);

            SilverbackBuilder.Services
                .AddScoped<IOutboundQueueConsumer, TQueueConsumer>();

            return this;
        }

        public IBrokerOptionsBuilder AddDbOutboundWorker(
            TimeSpan? interval = null,
            bool enforceMessageOrder = true,
            int readPackageSize = 100,
            DistributedLockSettings distributedLockSettings = null)
        {
            AddOutboundWorker<DbOutboundQueueConsumer>(
                interval,
                enforceMessageOrder,
                readPackageSize,
                distributedLockSettings);

            return this;
        }

        public IBrokerOptionsBuilder AddDbOutboundWorker(
            DistributedLockSettings distributedLockSettings,
            TimeSpan? interval = null,
            bool enforceMessageOrder = true,
            int readPackageSize = 100,
            bool removeProduced = true) =>
            AddDbOutboundWorker(interval, enforceMessageOrder, readPackageSize, distributedLockSettings);

        #endregion

        #region AddChunkStore

        public IBrokerOptionsBuilder AddChunkStore<TStore>(
            TimeSpan? retention = null,
            TimeSpan? cleanupInterval = null,
            DistributedLockSettings distributedLockSettings = null)
            where TStore : class, IChunkStore
        {
            distributedLockSettings ??= new DistributedLockSettings();
            distributedLockSettings.ResourceName ??= "ChunkStoreCleaner";
            
            SilverbackBuilder.Services
                .AddScoped<IChunkStore, TStore>()
                .AddSingleton(serviceProvider => new ChunkStoreCleaner(
                    retention ?? TimeSpan.FromDays(1),
                    serviceProvider.GetRequiredService<IServiceScopeFactory>(),
                    serviceProvider.GetRequiredService<ILogger<ChunkStoreCleaner>>()))
                .AddSingleton<IHostedService>(serviceProvider => new ChunkStoreCleanerService(
                    cleanupInterval ?? TimeSpan.FromMinutes(10),
                    serviceProvider.GetRequiredService<ChunkStoreCleaner>(),
                    distributedLockSettings,
                    serviceProvider.GetService<IDistributedLockManager>() ?? new NullLockManager(),
                    serviceProvider.GetRequiredService<ILogger<ChunkStoreCleanerService>>()));

            return this;
        }

        public IBrokerOptionsBuilder AddDbChunkStore(
            TimeSpan? retention = null,
            TimeSpan? cleanupInterval = null,
            DistributedLockSettings distributedLockSettings = null) =>
            AddChunkStore<DbChunkStore>(retention, cleanupInterval, distributedLockSettings);

        public IBrokerOptionsBuilder AddInMemoryChunkStore(
            TimeSpan? retention = null,
            TimeSpan? cleanupInterval = null) =>
            AddChunkStore<InMemoryChunkStore>(retention, cleanupInterval, DistributedLockSettings.NoLock);

        #endregion

        #region AddMessageIdProvider

#pragma warning disable 618
        [Obsolete("This feature will be removed in a future release. Please use a behavior instead.")]
        public IBrokerOptionsBuilder AddMessageIdProvider<TProvider>()
            where TProvider : class, IMessageIdProvider
        {
            SilverbackBuilder.Services.AddSingleton<IMessageIdProvider, TProvider>();

            return this;
        }
#pragma warning restore 618

        #endregion

        #region RegisterConfigurator

        public IBrokerOptionsBuilder RegisterConfigurator<TConfigurator>()
            where TConfigurator : class, IEndpointsConfigurator
        {
            SilverbackBuilder.Services.AddTransient<IEndpointsConfigurator, TConfigurator>();

            return this;
        }

        public IBrokerOptionsBuilder RegisterConfigurator(Type configuratorType)
        {
            SilverbackBuilder.Services.AddTransient(typeof(IEndpointsConfigurator), configuratorType);

            return this;
        }

        public IBrokerOptionsBuilder RegisterConfigurator(
            Func<IServiceProvider, IEndpointsConfigurator> implementationFactory)
        {
            SilverbackBuilder.Services.AddTransient(implementationFactory);

            return this;
        }

        #endregion

        #region AddSingletonBrokerBehavior

        public IBrokerOptionsBuilder AddSingletonBrokerBehavior(Type behaviorType)
        {
            SilverbackBuilder.Services.AddSingletonBrokerBehavior(behaviorType);
            return this;
        }

        public IBrokerOptionsBuilder AddSingletonBrokerBehavior<TBehavior>()
            where TBehavior : class, IBrokerBehavior
        {
            SilverbackBuilder.Services.AddSingletonBrokerBehavior<TBehavior>();
            return this;
        }

        public IBrokerOptionsBuilder AddSingletonBrokerBehavior(
            Func<IServiceProvider, IBrokerBehavior> implementationFactory)
        {
            SilverbackBuilder.Services.AddSingletonBrokerBehavior(implementationFactory);
            return this;
        }

        public IBrokerOptionsBuilder AddSingletonBrokerBehavior(
            IBrokerBehavior implementationInstance)
        {
            SilverbackBuilder.Services.AddSingletonBrokerBehavior(implementationInstance);
            return this;
        }

        #endregion

        #region AddSingletonOutboundRouter

        public IBrokerOptionsBuilder AddSingletonOutboundRouter(Type routerType)
        {
            SilverbackBuilder.Services.AddSingletonOutboundRouter(routerType);
            return this;
        }

        public IBrokerOptionsBuilder AddSingletonOutboundRouter<TRouter>()
            where TRouter : class, IOutboundRouter
        {
            SilverbackBuilder.Services.AddSingletonOutboundRouter<TRouter>();
            return this;
        }

        public IBrokerOptionsBuilder AddSingletonOutboundRouter(
            Func<IServiceProvider, IOutboundRouter> implementationFactory)
        {
            SilverbackBuilder.Services.AddSingletonOutboundRouter(implementationFactory);
            return this;
        }

        public IBrokerOptionsBuilder AddSingletonOutboundRouter(
            IOutboundRouter implementationInstance)
        {
            SilverbackBuilder.Services.AddSingletonOutboundRouter(implementationInstance);
            return this;
        }

        #endregion

        #region Defaults

        internal void CompleteWithDefaults()
        {
            if (!SilverbackBuilder.Services.ContainsAny<IInboundConnector>())
                AddInboundConnector();

            if (!SilverbackBuilder.Services.ContainsAny<IOutboundConnector>())
                AddOutboundConnector();
        }

        #endregion
    }
}