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
using Silverback.Messaging.Broker;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Connectors.Behaviors;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.LargeMessages;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Configuration
{
    [SuppressMessage("ReSharper", "UnusedMethodReturnValue.Local")]
    public class BrokerOptionsBuilder : IBrokerOptionsBuilder
    {
        public BrokerOptionsBuilder(ISilverbackBuilder silverbackBuilder)
        {
            SilverbackBuilder = silverbackBuilder;
        }

        /// <inheritdoc cref="IBrokerOptionsBuilder" />
        public ISilverbackBuilder SilverbackBuilder { get; }

        #region AddBroker

        /// <inheritdoc cref="IBrokerOptionsBuilder" />
        public IBrokerOptionsBuilder AddBroker<TBroker>()
            where TBroker : class, IBroker
        {
            SilverbackBuilder.Services
                .AddSingleton<IBroker, TBroker>()
                .AddSingleton(servicesProvider =>
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

        /// <inheritdoc cref="IBrokerOptionsBuilder" />
        public IBrokerOptionsBuilder AddInboundConnector<TConnector>()
            where TConnector : class, IInboundConnector
        {
            SilverbackBuilder.Services.AddSingleton<IInboundConnector, TConnector>();

            if (SilverbackBuilder.Services.All(s => s.ImplementationType != typeof(ErrorPolicyHelper)))
                SilverbackBuilder.Services.AddSingleton<ErrorPolicyHelper>();

            return this;
        }

        /// <inheritdoc cref="IBrokerOptionsBuilder" />
        public IBrokerOptionsBuilder AddInboundConnector() => AddInboundConnector<InboundConnector>();

        /// <inheritdoc cref="IBrokerOptionsBuilder" />
        public IBrokerOptionsBuilder AddLoggedInboundConnector(Func<IServiceProvider, IInboundLog> inboundLogFactory)
        {
            AddInboundConnector<LoggedInboundConnector>();
            SilverbackBuilder.Services.AddScoped(inboundLogFactory);
            return this;
        }

        /// <inheritdoc cref="IBrokerOptionsBuilder" />
        public IBrokerOptionsBuilder AddOffsetStoredInboundConnector(
            Func<IServiceProvider, IOffsetStore> offsetStoreFactory)
        {
            AddInboundConnector<OffsetStoredInboundConnector>();
            SilverbackBuilder.Services.AddScoped(offsetStoreFactory);
            return this;
        }

        /// <inheritdoc cref="IBrokerOptionsBuilder" />
        public IBrokerOptionsBuilder AddDbLoggedInboundConnector() =>
            AddLoggedInboundConnector(s =>
                new DbInboundLog(s.GetRequiredService<IDbContext>(),
                    s.GetRequiredService<MessageIdProvider>()));

        /// <inheritdoc cref="IBrokerOptionsBuilder" />
        public IBrokerOptionsBuilder AddDbOffsetStoredInboundConnector() =>
            AddOffsetStoredInboundConnector(s =>
                new DbOffsetStore(s.GetRequiredService<IDbContext>()));

        #endregion

        #region AddOutboundConnector

        /// <inheritdoc cref="IBrokerOptionsBuilder" />
        public IBrokerOptionsBuilder AddOutboundConnector<TConnector>()
            where TConnector : class, IOutboundConnector
        {
            if (SilverbackBuilder.Services.All(s => s.ServiceType != typeof(IOutboundRoutingConfiguration)))
            {
                SilverbackBuilder.Services.AddSingleton<IOutboundRoutingConfiguration, OutboundRoutingConfiguration>();
                SilverbackBuilder
                    .AddScopedBehavior<OutboundRoutingBehavior>()
                    .AddScopedBehavior<OutboundProducingBehavior>();
            }

            SilverbackBuilder.Services.AddScoped<IOutboundConnector, TConnector>();

            return this;
        }

        /// <inheritdoc cref="IBrokerOptionsBuilder" />
        public IBrokerOptionsBuilder AddOutboundConnector() => AddOutboundConnector<OutboundConnector>();

        /// <inheritdoc cref="IBrokerOptionsBuilder" />
        public IBrokerOptionsBuilder AddDeferredOutboundConnector(
            Func<IServiceProvider, IOutboundQueueProducer> outboundQueueProducerFactory)
        {
            AddOutboundConnector<DeferredOutboundConnector>();
            SilverbackBuilder.AddScopedSubscriber<DeferredOutboundConnectorTransactionManager>();
            SilverbackBuilder.Services.AddScoped(outboundQueueProducerFactory);

            return this;
        }

        /// <inheritdoc cref="IBrokerOptionsBuilder" />
        public IBrokerOptionsBuilder AddDbOutboundConnector() =>
            AddDeferredOutboundConnector(s =>
                new DbOutboundQueueProducer(s.GetRequiredService<IDbContext>()));

        #endregion

        #region AddOutboundWorker

        /// <inheritdoc cref="IBrokerOptionsBuilder" />
        // TODO: Test
        public IBrokerOptionsBuilder AddOutboundWorker(
            Func<IServiceProvider, IOutboundQueueConsumer> outboundQueueConsumerFactory,
            TimeSpan? interval = null,
            bool enforceMessageOrder = true,
            int readPackageSize = 100) =>
            AddOutboundWorker(outboundQueueConsumerFactory, new DistributedLockSettings(), interval,
                enforceMessageOrder, readPackageSize);

        /// <inheritdoc cref="IBrokerOptionsBuilder" />
        // TODO: Test
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

        /// <inheritdoc cref="IBrokerOptionsBuilder" />
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

        /// <inheritdoc cref="IBrokerOptionsBuilder" />
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

        /// <inheritdoc cref="IBrokerOptionsBuilder" />
        public IBrokerOptionsBuilder AddChunkStore<TStore>()
            where TStore : class, IChunkStore
        {
            if (SilverbackBuilder.Services.All(s => s.ServiceType != typeof(ChunkConsumer)))
            {
                SilverbackBuilder.Services.AddScoped<ChunkConsumer>();
            }

            SilverbackBuilder.Services.AddScoped<IChunkStore, TStore>();

            return this;
        }

        /// <inheritdoc cref="IBrokerOptionsBuilder" />
        public IBrokerOptionsBuilder AddDbChunkStore() =>
            AddChunkStore<DbChunkStore>();

        #endregion

        #region AddMessageIdProvider

        /// <inheritdoc cref="IBrokerOptionsBuilder" />
        public IBrokerOptionsBuilder AddMessageIdProvider<TProvider>()
            where TProvider : class, IMessageIdProvider
        {
            SilverbackBuilder.Services.AddSingleton<IMessageIdProvider, TProvider>();

            return this;
        }

        #endregion

        #region RegisterConfigurator

        /// <inheritdoc cref="IBrokerOptionsBuilder" />
        public IBrokerOptionsBuilder RegisterConfigurator<TConfigurator>()
            where TConfigurator : class, IEndpointsConfigurator
        {
            SilverbackBuilder.Services.AddTransient<IEndpointsConfigurator, TConfigurator>();

            return this;
        }

        /// <inheritdoc cref="IBrokerOptionsBuilder" />
        public IBrokerOptionsBuilder RegisterConfigurator(Type configuratorType)
        {
            SilverbackBuilder.Services.AddTransient(typeof(IEndpointsConfigurator), configuratorType);

            return this;
        }

        /// <inheritdoc cref="IBrokerOptionsBuilder" />
        public IBrokerOptionsBuilder RegisterConfigurator(
            Func<IServiceProvider, IEndpointsConfigurator> implementationFactory)
        {
            SilverbackBuilder.Services.AddTransient(implementationFactory);

            return this;
        }

        #endregion

        #region Defaults

        internal void CompleteWithDefaults()
        {
            AddMessageIdProvider<DefaultPropertiesMessageIdProvider>();

            if (SilverbackBuilder.Services.All(s => s.ServiceType != typeof(IInboundConnector)))
                AddInboundConnector();

            if (SilverbackBuilder.Services.All(s => s.ServiceType != typeof(IOutboundRoutingConfiguration)))
                AddOutboundConnector();
        }

        #endregion
    }
}