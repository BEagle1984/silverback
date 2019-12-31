// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
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
    public class BrokerOptionsBuilder
    {
        internal readonly ISilverbackBuilder SilverbackBuilder;

        public BrokerOptionsBuilder(ISilverbackBuilder silverbackBuilder)
        {
            SilverbackBuilder = silverbackBuilder;
        }

        #region AddInboundConnector

        /// <summary>
        ///     Adds a connector to subscribe to a message broker and forward the incoming integration messages to the internal
        ///     bus.
        /// </summary>
        public BrokerOptionsBuilder AddInboundConnector<TConnector>()
            where TConnector : class, IInboundConnector
        {
            SilverbackBuilder.Services.AddSingleton<IInboundConnector, TConnector>();

            if (SilverbackBuilder.Services.All(s => s.ImplementationType != typeof(InboundMessageUnwrapper)))
                SilverbackBuilder.AddScopedSubscriber<InboundMessageUnwrapper>();

            if (SilverbackBuilder.Services.All(s => s.ImplementationType != typeof(ErrorPolicyHelper)))
                SilverbackBuilder.Services.AddSingleton<ErrorPolicyHelper>();

            return this;
        }

        /// <summary>
        ///     Adds a connector to subscribe to a message broker and forward the incoming integration messages to the internal
        ///     bus.
        /// </summary>
        public BrokerOptionsBuilder AddInboundConnector() => AddInboundConnector<InboundConnector>();

        /// <summary>
        ///     Adds a connector to subscribe to a message broker and forward the incoming integration messages to the internal
        ///     bus. This implementation logs the incoming messages and prevents duplicated processing of the same message.
        /// </summary>
        public BrokerOptionsBuilder AddLoggedInboundConnector(Func<IServiceProvider, IInboundLog> inboundLogFactory)
        {
            AddInboundConnector<LoggedInboundConnector>();
            SilverbackBuilder.Services.AddScoped(inboundLogFactory);
            return this;
        }

        /// <summary>
        ///     Adds a connector to subscribe to a message broker and forward the incoming integration messages to the internal
        ///     bus. This implementation stores the offset of the latest consumed messages and prevents duplicated processing of
        ///     the
        ///     same message.
        /// </summary>
        public BrokerOptionsBuilder AddOffsetStoredInboundConnector(
            Func<IServiceProvider, IOffsetStore> offsetStoreFactory)
        {
            AddInboundConnector<OffsetStoredInboundConnector>();
            SilverbackBuilder.Services.AddScoped(offsetStoreFactory);
            return this;
        }

        /// <summary>
        ///     Adds a connector to subscribe to a message broker and forward the incoming integration messages to the internal
        ///     bus. This implementation logs the incoming messages in the database and prevents duplicated processing of the same
        ///     message.
        /// </summary>
        public BrokerOptionsBuilder AddDbLoggedInboundConnector() =>
            AddLoggedInboundConnector(s =>
                new DbInboundLog(s.GetRequiredService<IDbContext>(),
                    s.GetRequiredService<MessageKeyProvider>()));

        /// <summary>
        ///     Adds a connector to subscribe to a message broker and forward the incoming integration messages to the internal
        ///     bus. This implementation stores the offset of the latest consumed messages in the database and prevents duplicated
        ///     processing of the same message.
        /// </summary>
        public BrokerOptionsBuilder AddDbOffsetStoredInboundConnector() =>
            AddOffsetStoredInboundConnector(s =>
                new DbOffsetStore(s.GetRequiredService<IDbContext>()));

        #endregion

        #region AddOutboundConnector

        /// <summary>
        ///     Adds a connector to publish the integration messages to the configured message broker.
        /// </summary>
        public BrokerOptionsBuilder AddOutboundConnector<TConnector>()
            where TConnector : class, IOutboundConnector
        {
            if (SilverbackBuilder.Services.All(s => s.ServiceType != typeof(IOutboundRoutingConfiguration)))
            {
                SilverbackBuilder.Services.AddSingleton<IOutboundRoutingConfiguration, OutboundRoutingConfiguration>();
                SilverbackBuilder.AddScopedBehavior<OutboundProducingBehavior>();
                SilverbackBuilder.AddScopedBehavior<OutboundRoutingBehavior>();
            }

            SilverbackBuilder.Services.AddScoped<IOutboundConnector, TConnector>();

            return this;
        }

        /// <summary>
        ///     Adds a connector to publish the integration messages to the configured message broker.
        /// </summary>
        public BrokerOptionsBuilder AddOutboundConnector() => AddOutboundConnector<OutboundConnector>();

        /// <summary>
        ///     Adds a connector to publish the integration messages to the configured message broker.
        ///     This implementation stores the outbound messages into an intermediate queue.
        /// </summary>
        public BrokerOptionsBuilder AddDeferredOutboundConnector(
            Func<IServiceProvider, IOutboundQueueProducer> outboundQueueProducerFactory)
        {
            AddOutboundConnector<DeferredOutboundConnector>();
            SilverbackBuilder.AddScopedSubscriber<DeferredOutboundConnectorTransactionManager>();
            SilverbackBuilder.Services.AddScoped(outboundQueueProducerFactory);

            return this;
        }

        /// <summary>
        ///     Adds a connector to publish the integration messages to the configured message broker.
        ///     This implementation stores the outbound messages into an intermediate queue in the database and
        ///     it is therefore fully transactional.
        /// </summary>
        public BrokerOptionsBuilder AddDbOutboundConnector() =>
            AddDeferredOutboundConnector(s =>
                new DbOutboundQueueProducer(s.GetRequiredService<IDbContext>()));

        /// <summary>
        ///     Adds an <see cref="OutboundQueueWorker" /> to publish the queued messages to the configured broker.
        /// </summary>
        /// <param name="interval">The interval between each run (default is 500ms).</param>
        /// <param name="enforceMessageOrder">
        ///     if set to <c>true</c> the message order will be preserved (no message will be
        ///     skipped).
        /// </param>
        /// <param name="readPackageSize">The number of messages to be loaded from the queue at once.</param>
        /// <param name="removeProduced">
        ///     if set to <c>true</c> the messages will be removed from the database immediately after
        ///     being produced.
        /// </param>
        public BrokerOptionsBuilder AddDbOutboundWorker(
            TimeSpan? interval = null,
            bool enforceMessageOrder = true,
            int readPackageSize = 100,
            bool removeProduced = true)
        {
            return AddDbOutboundWorker(new DistributedLockSettings(), interval,
                enforceMessageOrder, readPackageSize, removeProduced);
        }

        #endregion

        #region AddOutboundWorker

        /// <summary>
        ///     Adds an <see cref="OutboundQueueWorker" /> to publish the queued messages to the configured broker.
        /// </summary>
        /// <param name="outboundQueueConsumerFactory"></param>
        /// <param name="interval">The interval between each run (default is 500ms).</param>
        /// <param name="enforceMessageOrder">
        ///     If set to <c>true</c> the message order will be preserved (no message will be
        ///     skipped).
        /// </param>
        /// <param name="readPackageSize">The number of messages to be loaded from the queue at once.</param>
        // TODO: Test
        public BrokerOptionsBuilder AddOutboundWorker(
            Func<IServiceProvider, IOutboundQueueConsumer> outboundQueueConsumerFactory,
            TimeSpan? interval = null,
            bool enforceMessageOrder = true,
            int readPackageSize = 100) =>
            AddOutboundWorker(outboundQueueConsumerFactory, new DistributedLockSettings(), interval,
                enforceMessageOrder, readPackageSize);

        /// <summary>
        ///     Adds an <see cref="OutboundQueueWorker" /> to publish the queued messages to the configured broker.
        /// </summary>
        /// <param name="outboundQueueConsumerFactory"></param>
        /// <param name="distributedLockSettings">The settings for the locking mechanism.</param>
        /// <param name="interval">The interval between each run (default is 500ms).</param>
        /// <param name="enforceMessageOrder">
        ///     If set to <c>true</c> the message order will be preserved (no message will be
        ///     skipped).
        /// </param>
        /// <param name="readPackageSize">The number of messages to be loaded from the queue at once.</param>
        // TODO: Test
        public BrokerOptionsBuilder AddOutboundWorker(
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

        internal BrokerOptionsBuilder AddOutboundWorker(
            TimeSpan interval,
            DistributedLockSettings distributedLockSettings,
            bool enforceMessageOrder,
            int readPackageSize)
        {
            SilverbackBuilder.Services
                .AddSingleton<IOutboundQueueWorker>(s => new OutboundQueueWorker(
                    s.GetRequiredService<IServiceProvider>(),
                    s.GetRequiredService<IBroker>(),
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

        /// <summary>
        ///     Adds an <see cref="OutboundQueueWorker" /> to publish the queued messages to the configured broker.
        /// </summary>
        /// <param name="distributedLockSettings">The settings for the locking mechanism.</param>
        /// <param name="interval">The interval between each run (default is 500ms).</param>
        /// <param name="enforceMessageOrder">
        ///     if set to <c>true</c> the message order will be preserved (no message will be
        ///     skipped).
        /// </param>
        /// <param name="readPackageSize">The number of messages to be loaded from the queue at once.</param>
        /// <param name="removeProduced">
        ///     if set to <c>true</c> the messages will be removed from the database immediately after
        ///     being produced.
        /// </param>
        public BrokerOptionsBuilder AddDbOutboundWorker(
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

        /// <summary>
        ///     Adds a chunk store to temporary save the message chunks until the full message has been received.
        /// </summary>
        public BrokerOptionsBuilder AddChunkStore<TStore>()
            where TStore : class, IChunkStore
        {
            if (SilverbackBuilder.Services.All(s => s.ServiceType != typeof(ChunkConsumer)))
            {
                SilverbackBuilder.Services.AddScoped<ChunkConsumer>();
            }

            SilverbackBuilder.Services.AddScoped<IChunkStore, TStore>();

            return this;
        }

        /// <summary>
        ///     Adds a chunk store to temporary save the message chunks until the full message has been received.
        ///     This implementation stores the message chunks in the database.
        /// </summary>
        public BrokerOptionsBuilder AddDbChunkStore() =>
            AddChunkStore<DbChunkStore>();

        #endregion

        #region AddMessageKeyProvider

        /// <summary>
        ///     Adds a message key provider of type <typeparamref name="TProvider" /> to be used
        ///     to auto-generate the unique id of the integration messages.
        /// </summary>
        /// <typeparam name="TProvider">The type of the <see cref="IMessageKeyProvider" /> to add.</typeparam>
        /// <returns></returns>
        public BrokerOptionsBuilder AddMessageKeyProvider<TProvider>()
            where TProvider : class, IMessageKeyProvider
        {
            SilverbackBuilder.Services.AddSingleton<IMessageKeyProvider, TProvider>();

            return this;
        }

        #endregion

        #region RegisterConfigurator

        /// <summary>
        ///     Registers an <see cref="IEndpointsConfigurator" /> to be used to setup the broker endpoints.
        /// </summary>
        /// <typeparam name="TConfigurator">The type of the <see cref="IEndpointsConfigurator" /> to add.</typeparam>
        /// <returns></returns>
        public BrokerOptionsBuilder RegisterConfigurator<TConfigurator>()
            where TConfigurator : class, IEndpointsConfigurator
        {
            SilverbackBuilder.Services.AddTransient<IEndpointsConfigurator, TConfigurator>();

            return this;
        }

        /// <summary>
        ///     Registers an <see cref="IEndpointsConfigurator" /> to be used to setup the broker endpoints.
        /// </summary>
        /// <param name="configuratorType">The type of the <see cref="IEndpointsConfigurator" /> to add.</param>
        /// <returns></returns>
        public BrokerOptionsBuilder RegisterConfigurator(Type configuratorType)
        {
            SilverbackBuilder.Services.AddTransient(typeof(IEndpointsConfigurator), configuratorType);

            return this;
        }

        /// <summary>
        ///     Registers an <see cref="IEndpointsConfigurator" /> to be used to setup the broker endpoints.
        /// </summary>
        /// <param name="implementationFactory">The factory that creates the <see cref="IEndpointsConfigurator" /> to add.</param>
        /// <returns></returns>
        public BrokerOptionsBuilder RegisterConfigurator(
            Func<IServiceProvider, IEndpointsConfigurator> implementationFactory)
        {
            SilverbackBuilder.Services.AddTransient(implementationFactory);

            return this;
        }

        #endregion

        #region Defaults

        internal void CompleteWithDefaults() => SetDefaults();

        /// <summary>
        ///     Sets the default values (especially for the options that have not been explicitly set
        ///     by the user).
        /// </summary>
        protected virtual void SetDefaults()
        {
            AddMessageKeyProvider<DefaultPropertiesMessageKeyProvider>();

            if (SilverbackBuilder.Services.All(s => s.ServiceType != typeof(IInboundConnector)))
                AddInboundConnector();

            if (SilverbackBuilder.Services.All(s => s.ServiceType != typeof(IOutboundRoutingConfiguration)))
                AddOutboundConnector();
        }

        #endregion
    }
}