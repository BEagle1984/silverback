// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Background;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.LargeMessages;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Configuration
{
    public interface IBrokerOptionsBuilder
    {
        /// <summary>
        ///     Gets a reference to the related <see cref="ISilverbackBuilder" /> instance.
        /// </summary>
        ISilverbackBuilder SilverbackBuilder { get; }

        #region AddBroker

        /// <summary>
        ///     Adds the specified <see cref="IBroker" /> implementation to allow producing and consuming messages.
        /// </summary>
        /// <typeparam name="TBroker">The type of the <see cref="IBroker" /> implementation to add.</typeparam>
        /// <returns></returns>
        IBrokerOptionsBuilder AddBroker<TBroker>()
            where TBroker : class, IBroker;

        #endregion

        #region AddInboundConnector

        /// <summary>
        ///     Adds a connector to subscribe to a message broker and forward the incoming integration messages to the
        ///     internal bus.
        /// </summary>
        IBrokerOptionsBuilder AddInboundConnector<TConnector>()
            where TConnector : class, IInboundConnector;

        /// <summary>
        ///     Adds a connector to subscribe to a message broker and forward the incoming integration messages to the
        ///     internal bus.
        /// </summary>
        IBrokerOptionsBuilder AddInboundConnector();

        /// <summary>
        ///     Adds a connector to subscribe to a message broker and forward the incoming integration messages to the
        ///     internal bus. This implementation logs the incoming messages and prevents duplicated processing of the
        ///     same message.
        /// </summary>
        IBrokerOptionsBuilder AddLoggedInboundConnector();

        /// <summary>
        ///     Adds a connector to subscribe to a message broker and forward the incoming integration messages to the
        ///     internal bus. This implementation logs the incoming messages and prevents duplicated processing of the
        ///     same message.
        /// </summary>
        /// <typeparam name="TLog">The type of the <see cref="IInboundLog" /> to be used.</typeparam>
        IBrokerOptionsBuilder AddLoggedInboundConnector<TLog>()
            where TLog : class, IInboundLog;

        /// <summary>
        ///     Adds a connector to subscribe to a message broker and forward the incoming integration messages to the
        ///     internal bus. This implementation stores the offset of the latest consumed messages and prevents
        ///     duplicated processing of the same message.
        /// </summary>
        IBrokerOptionsBuilder AddOffsetStoredInboundConnector();

        /// <summary>
        ///     Adds a connector to subscribe to a message broker and forward the incoming integration messages to the
        ///     internal bus. This implementation stores the offset of the latest consumed messages and prevents
        ///     duplicated processing of the same message.
        /// </summary>
        /// <typeparam name="TStore">The type of the <see cref="IOffsetStore" /> to be used.</typeparam>
        IBrokerOptionsBuilder AddOffsetStoredInboundConnector<TStore>()
            where TStore : class, IOffsetStore;

        /// <summary>
        ///     Adds a connector to subscribe to a message broker and forward the incoming integration messages to the
        ///     internal bus. This implementation logs the incoming messages in the database and prevents duplicated
        ///     processing of the same message.
        /// </summary>
        IBrokerOptionsBuilder AddDbLoggedInboundConnector();

        /// <summary>
        ///     Adds a connector to subscribe to a message broker and forward the incoming integration messages to the
        ///     internal bus. This implementation stores the offset of the latest consumed messages in the database and
        ///     prevents duplicated processing of the same message.
        /// </summary>
        IBrokerOptionsBuilder AddDbOffsetStoredInboundConnector();

        #endregion

        #region AddOutboundConnector

        /// <summary>
        ///     Adds a connector to publish the integration messages to the configured message broker.
        /// </summary>
        IBrokerOptionsBuilder AddOutboundConnector<TConnector>()
            where TConnector : class, IOutboundConnector;

        /// <summary>
        ///     Adds a connector to publish the integration messages to the configured message broker.
        /// </summary>
        IBrokerOptionsBuilder AddOutboundConnector();

        /// <summary>
        ///     Adds a connector to publish the integration messages to the configured message broker.
        ///     This implementation stores the outbound messages into an intermediate queue.
        /// </summary>
        IBrokerOptionsBuilder AddDeferredOutboundConnector();

        /// <summary>
        ///     Adds a connector to publish the integration messages to the configured message broker.
        ///     This implementation stores the outbound messages into an intermediate queue.
        /// </summary>
        /// <typeparam name="TQueueProducer">The type of the <see cref="IOutboundQueueProducer" /> to be used.</typeparam>
        IBrokerOptionsBuilder AddDeferredOutboundConnector<TQueueProducer>()
            where TQueueProducer : class, IOutboundQueueProducer;

        /// <summary>
        ///     Adds a connector to publish the integration messages to the configured message broker.
        ///     This implementation stores the outbound messages into an intermediate queue in the database and
        ///     it is therefore fully transactional.
        /// </summary>
        IBrokerOptionsBuilder AddDbOutboundConnector();

        #endregion

        #region AddOutboundWorker

        /// <summary>
        ///     Adds an <see cref="OutboundQueueWorker" /> to publish the queued messages to the configured broker.
        /// </summary>
        /// <param name="interval">The interval between each run (default is 500ms).</param>
        /// <param name="enforceMessageOrder">
        ///     If set to <c>true</c> the message order will be ensured, retrying the same message until it can be
        ///     successfully produced.
        /// </param>
        /// <param name="readPackageSize">The number of messages to be loaded from the queue at once.</param>
        /// <param name="distributedLockSettings">
        ///     The settings for the locking mechanism (default settings will be used if not specified).
        /// </param>
        public IBrokerOptionsBuilder AddOutboundWorker(
            TimeSpan? interval = null,
            bool enforceMessageOrder = true,
            int readPackageSize = 100,
            DistributedLockSettings distributedLockSettings = null);

        /// <summary>
        ///     Adds an <see cref="OutboundQueueWorker" /> to publish the queued messages to the configured broker.
        /// </summary>
        /// <typeparam name="TQueueConsumer">The type of the <see cref="IOutboundQueueConsumer" /> to be used.</typeparam>
        /// <param name="interval">The interval between each run (default is 500ms).</param>
        /// <param name="enforceMessageOrder">
        ///     If set to <c>true</c> the message order will be ensured, retrying the same message until it can be
        ///     successfully produced.
        /// </param>
        /// <param name="readPackageSize">The number of messages to be loaded from the queue at once.</param>
        /// <param name="distributedLockSettings">
        ///     The settings for the locking mechanism (default settings will be used if not specified).
        /// </param>
        IBrokerOptionsBuilder AddOutboundWorker<TQueueConsumer>(
            TimeSpan? interval = null,
            bool enforceMessageOrder = true,
            int readPackageSize = 100,
            DistributedLockSettings distributedLockSettings = null)
            where TQueueConsumer : class, IOutboundQueueConsumer;

        /// <summary>
        ///     Adds an <see cref="OutboundQueueWorker" /> to publish the queued messages to the configured broker.
        /// </summary>
        /// <param name="interval">The interval between each run (default is 500ms).</param>
        /// <param name="enforceMessageOrder">
        ///     If set to <c>true</c> the message order will be ensured, retrying the same message until it can be
        ///     successfully produced.
        /// </param>
        /// <param name="readPackageSize">The number of messages to be loaded from the queue at once.</param>
        /// <param name="distributedLockSettings">
        ///     The settings for the locking mechanism (default settings will be used if not specified).
        /// </param>
        IBrokerOptionsBuilder AddDbOutboundWorker(
            TimeSpan? interval = null,
            bool enforceMessageOrder = true,
            int readPackageSize = 100,
            DistributedLockSettings distributedLockSettings = null);

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
        [Obsolete("Use the other AddDbOutboundWorker overload.")]
        IBrokerOptionsBuilder AddDbOutboundWorker(
            DistributedLockSettings distributedLockSettings,
            TimeSpan? interval = null,
            bool enforceMessageOrder = true,
            int readPackageSize = 100,
            bool removeProduced = true);
        
        #endregion

        #region AddChunkStore

        /// <summary>
        ///     Adds a chunk store to temporary save the message chunks until the full message has been received.
        /// </summary>
        /// <param name="retention">
        ///     The retention time of the stored chunks. The chunks will be discarded after this time is elapsed
        ///     (default is 1 hour).
        /// </param>
        /// <param name="cleanupInterval">The interval between each cleanup (default is 10 minutes).</param>
        /// <param name="distributedLockSettings">
        ///     The settings for the locking mechanism (default settings will be used if not specified).
        /// </param>
        IBrokerOptionsBuilder AddChunkStore<TStore>(
            TimeSpan? retention = null,
            TimeSpan? cleanupInterval = null,
            DistributedLockSettings distributedLockSettings = null)
            where TStore : class, IChunkStore;

        /// <summary>
        ///     Adds a chunk store to temporary save the message chunks until the full message has been received.
        ///     This implementation stores the message chunks in memory.
        /// </summary>
        /// <param name="retention">
        ///     The retention time of the stored chunks. The chunks will be discarded after this time is elapsed
        ///     (default is 1 hour).
        /// </param>
        /// <param name="cleanupInterval">The interval between each cleanup (default is 10 minutes).</param>
        IBrokerOptionsBuilder AddInMemoryChunkStore(
            TimeSpan? retention = null,
            TimeSpan? cleanupInterval = null);

        /// <summary>
        ///     Adds a chunk store to temporary save the message chunks until the full message has been received.
        ///     This implementation stores the message chunks in the database.
        /// </summary>
        /// <param name="retention">
        ///     The retention time of the stored chunks. The chunks will be discarded after this time is elapsed
        ///     (default is 1 hour).
        /// </param>
        /// <param name="cleanupInterval">The interval between each cleanup (default is 10 minutes).</param>
        /// <param name="distributedLockSettings">
        ///     The settings for the locking mechanism (default settings will be used if not specified).
        /// </param>
        IBrokerOptionsBuilder AddDbChunkStore(
            TimeSpan? retention = null,
            TimeSpan? cleanupInterval = null,
            DistributedLockSettings distributedLockSettings = null);

        #endregion

        #region AddMessageIdProvider

        /// <summary>
        ///     Adds a message id provider of type <typeparamref name="TProvider" /> to be used
        ///     to auto-generate the unique id of the integration messages.
        /// </summary>
        /// <typeparam name="TProvider">The type of the <see cref="IMessageIdProvider" /> to add.</typeparam>
        /// <returns></returns>
        [Obsolete("This feature will be removed in a future release. Please use a behavior instead.")]
        IBrokerOptionsBuilder AddMessageIdProvider<TProvider>()
            where TProvider : class, IMessageIdProvider;

        #endregion

        #region RegisterConfigurator

        /// <summary>
        ///     Registers an <see cref="IEndpointsConfigurator" /> to be used to setup the broker endpoints.
        /// </summary>
        /// <typeparam name="TConfigurator">The type of the <see cref="IEndpointsConfigurator" /> to add.</typeparam>
        /// <returns></returns>
        IBrokerOptionsBuilder RegisterConfigurator<TConfigurator>()
            where TConfigurator : class, IEndpointsConfigurator;

        /// <summary>
        ///     Registers an <see cref="IEndpointsConfigurator" /> to be used to setup the broker endpoints.
        /// </summary>
        /// <param name="configuratorType">The type of the <see cref="IEndpointsConfigurator" /> to add.</param>
        /// <returns></returns>
        IBrokerOptionsBuilder RegisterConfigurator(Type configuratorType);

        /// <summary>
        ///     Registers an <see cref="IEndpointsConfigurator" /> to be used to setup the broker endpoints.
        /// </summary>
        /// <param name="implementationFactory">The factory that creates the <see cref="IEndpointsConfigurator" /> to add.</param>
        /// <returns></returns>
        IBrokerOptionsBuilder RegisterConfigurator(
            Func<IServiceProvider, IEndpointsConfigurator> implementationFactory);

        #endregion

        #region AddSingletonBrokerBehavior

        /// <summary>
        ///     Adds a singleton behavior of the type specified in <paramref name="behaviorType" /> to the
        ///     <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="behaviorType">The type of the behavior to register and the implementation to use.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        IBrokerOptionsBuilder AddSingletonBrokerBehavior(Type behaviorType);

        /// <summary>
        ///     Adds a singleton behavior of the type specified in <typeparamref name="TBehavior" /> to the
        ///     <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBehavior">The type of the behavior to add.</typeparam>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        IBrokerOptionsBuilder AddSingletonBrokerBehavior<TBehavior>()
            where TBehavior : class, IBrokerBehavior;

        /// <summary>
        ///     Adds a singleton behavior with a
        ///     factory specified in <paramref name="implementationFactory" /> to the
        ///     <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        IBrokerOptionsBuilder AddSingletonBrokerBehavior(Func<IServiceProvider, IBrokerBehavior> implementationFactory);

        /// <summary>
        ///     Adds a singleton behavior with an
        ///     instance specified in <paramref name="implementationInstance" /> to the
        ///     <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="implementationInstance">The instance of the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        IBrokerOptionsBuilder AddSingletonBrokerBehavior(IBrokerBehavior implementationInstance);

        #endregion

        #region AddSingletonOutboundRouter

        /// <summary>
        ///     Adds a singleton outbound router of the type specified in <paramref name="routerType" /> to the
        ///     <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="routerType">The type of the outbound router to register and the implementation to use.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        IBrokerOptionsBuilder AddSingletonOutboundRouter(Type routerType);

        /// <summary>
        ///     Adds a singleton outbound router of the type specified in <typeparamref name="TRouter" /> to the
        ///     <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TRouter">The type of the outbound router to add.</typeparam>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        IBrokerOptionsBuilder AddSingletonOutboundRouter<TRouter>()
            where TRouter : class, IOutboundRouter;

        /// <summary>
        ///     Adds a singleton outbound router with a
        ///     factory specified in <paramref name="implementationFactory" /> to the
        ///     <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        IBrokerOptionsBuilder AddSingletonOutboundRouter(Func<IServiceProvider, IOutboundRouter> implementationFactory);

        /// <summary>
        ///     Adds a singleton outbound router with an
        ///     instance specified in <paramref name="implementationInstance" /> to the
        ///     <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="implementationInstance">The instance of the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        IBrokerOptionsBuilder AddSingletonOutboundRouter(IOutboundRouter implementationInstance);

        #endregion
    }
}