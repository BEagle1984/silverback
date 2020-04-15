// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Silverback.Background;
using Silverback.Database;
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
                    .AddSingletonBrokerBehavior<SerializerProducerBehavior>()
                    .AddSingletonBrokerBehavior<DeserializerConsumerBehavior>()
                    .AddSingletonBrokerBehavior<EncryptorProducerBehavior>()
                    .AddSingletonBrokerBehavior<DecryptorConsumerBehavior>()
                    .AddSingletonBrokerBehavior<HeadersWriterProducerBehavior>()
                    .AddSingletonBrokerBehavior<HeadersReaderConsumerBehavior>()
                    .AddSingleton<IMessageTransformerFactory, MessageTransformerFactory>()
                    .AddSingletonBrokerBehavior<ChunkSplitterProducerBehavior>()
                    .AddSingletonBrokerBehavior<ChunkAggregatorConsumerBehavior>()
                    .AddScoped<ChunkAggregator>()
                    .AddSingletonBrokerBehavior<BinaryFileHandlerProducerBehavior>()
                    .AddSingletonBrokerBehavior<BinaryFileHandlerConsumerBehavior>()
                    .AddSingletonBrokerBehavior<MessageIdInitializerProducerBehavior>()
                    .AddSingletonBrokerBehavior<InboundProcessorConsumerBehaviorFactory>()
                    .AddScopedSubscriber<ConsumerTransactionManager>();

#pragma warning disable 618
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

        public IBrokerOptionsBuilder AddLoggedInboundConnector(Func<IServiceProvider, IInboundLog> inboundLogFactory)
        {
            AddInboundConnector<LoggedInboundConnector>();
            SilverbackBuilder.Services.AddScoped(inboundLogFactory);
            return this;
        }

        public IBrokerOptionsBuilder AddOffsetStoredInboundConnector(
            Func<IServiceProvider, IOffsetStore> offsetStoreFactory)
        {
            AddInboundConnector<OffsetStoredInboundConnector>();
            SilverbackBuilder.Services.AddScoped(offsetStoreFactory);
            return this;
        }

        public IBrokerOptionsBuilder AddDbLoggedInboundConnector() =>
            AddLoggedInboundConnector(s =>
                new DbInboundLog(s.GetRequiredService<IDbContext>(),
                    s.GetRequiredService<MessageIdProvider>()));

        public IBrokerOptionsBuilder AddDbOffsetStoredInboundConnector() =>
            AddOffsetStoredInboundConnector(s =>
                new DbOffsetStore(s.GetRequiredService<IDbContext>()));

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

        public IBrokerOptionsBuilder AddDeferredOutboundConnector(
            Func<IServiceProvider, IOutboundQueueProducer> outboundQueueProducerFactory)
        {
            AddOutboundConnector<DeferredOutboundConnector>();
            SilverbackBuilder.AddScopedSubscriber<DeferredOutboundConnectorTransactionManager>();
            SilverbackBuilder.Services.AddScoped(outboundQueueProducerFactory);

            return this;
        }

        public IBrokerOptionsBuilder AddDbOutboundConnector() =>
            AddDeferredOutboundConnector(s =>
                new DbOutboundQueueProducer(s.GetRequiredService<IDbContext>()));

        #endregion

        #region AddOutboundWorker

        public IBrokerOptionsBuilder AddOutboundWorker(
            Func<IServiceProvider, IOutboundQueueConsumer> outboundQueueConsumerFactory,
            TimeSpan? interval = null,
            bool enforceMessageOrder = true,
            int readPackageSize = 100) =>
            AddOutboundWorker(outboundQueueConsumerFactory, new DistributedLockSettings(), interval,
                enforceMessageOrder, readPackageSize);

        public IBrokerOptionsBuilder AddOutboundWorker(
            Func<IServiceProvider, IOutboundQueueConsumer> outboundQueueConsumerFactory,
            DistributedLockSettings distributedLockSettings,
            TimeSpan? interval = null,
            bool enforceMessageOrder = true,
            int readPackageSize = 100)
        {
            if (outboundQueueConsumerFactory == null)
                throw new ArgumentNullException(nameof(outboundQueueConsumerFactory));
            if (distributedLockSettings == null) throw new ArgumentNullException(nameof(distributedLockSettings));

            if (string.IsNullOrEmpty(distributedLockSettings.ResourceName))
                distributedLockSettings.ResourceName = "OutboundQueueWorker";

            AddOutboundWorker(
                interval ?? TimeSpan.FromMilliseconds(500),
                distributedLockSettings,
                enforceMessageOrder,
                readPackageSize);

            SilverbackBuilder.Services
                .AddScoped(outboundQueueConsumerFactory);

            return this;
        }

        public IBrokerOptionsBuilder AddDbOutboundWorker(
            TimeSpan? interval = null,
            bool enforceMessageOrder = true,
            int readPackageSize = 100,
            bool removeProduced = true)
        {
            return AddDbOutboundWorker(new DistributedLockSettings(), interval,
                enforceMessageOrder, readPackageSize, removeProduced);
        }

        private BrokerOptionsBuilder AddOutboundWorker(
            TimeSpan interval,
            DistributedLockSettings distributedLockSettings,
            bool enforceMessageOrder,
            int readPackageSize)
        {
            SilverbackBuilder.Services
                .AddSingleton<IOutboundQueueWorker>(s => new OutboundQueueWorker(
                    s.GetRequiredService<IServiceProvider>(),
                    s.GetRequiredService<IBrokerCollection>(),
                    s.GetRequiredService<ILogger<OutboundQueueWorker>>(),
                    s.GetRequiredService<MessageLogger>(),
                    enforceMessageOrder, readPackageSize))
                .AddSingleton<IHostedService>(s => new OutboundQueueWorkerService(
                    interval,
                    s.GetRequiredService<IOutboundQueueWorker>(),
                    distributedLockSettings,
                    s.GetService<IDistributedLockManager>() ?? new NullLockManager(),
                    s.GetRequiredService<ILogger<OutboundQueueWorkerService>>()));

            return this;
        }

        public IBrokerOptionsBuilder AddDbOutboundWorker(
            DistributedLockSettings distributedLockSettings,
            TimeSpan? interval = null,
            bool enforceMessageOrder = true,
            int readPackageSize = 100,
            bool removeProduced = true)
        {
            if (distributedLockSettings == null) throw new ArgumentNullException(nameof(distributedLockSettings));

            if (string.IsNullOrEmpty(distributedLockSettings.ResourceName))
                distributedLockSettings.ResourceName = "DbOutboundQueueWorker";

            AddOutboundWorker(
                s => new DbOutboundQueueConsumer(s.GetRequiredService<IDbContext>(), removeProduced),
                distributedLockSettings, interval,
                enforceMessageOrder, readPackageSize);

            return this;
        }

        #endregion

        #region AddChunkStore

        public IBrokerOptionsBuilder AddChunkStore<TStore>()
            where TStore : class, IChunkStore
        {
            SilverbackBuilder.Services.AddScoped<IChunkStore, TStore>();

            return this;
        }

        public IBrokerOptionsBuilder AddDbChunkStore() =>
            AddChunkStore<DbChunkStore>();

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