// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Background;
using Silverback.Messaging.Broker;
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
        ///     Adds a connector to subscribe to a message broker and forward the incoming integration messages to the internal
        ///     bus.
        /// </summary>
        IBrokerOptionsBuilder AddInboundConnector<TConnector>()
            where TConnector : class, IInboundConnector;

        /// <summary>
        ///     Adds a connector to subscribe to a message broker and forward the incoming integration messages to the internal
        ///     bus.
        /// </summary>
        IBrokerOptionsBuilder AddInboundConnector();

        /// <summary>
        ///     Adds a connector to subscribe to a message broker and forward the incoming integration messages to the internal
        ///     bus. This implementation logs the incoming messages and prevents duplicated processing of the same message.
        /// </summary>
        IBrokerOptionsBuilder AddLoggedInboundConnector(Func<IServiceProvider, IInboundLog> inboundLogFactory);

        /// <summary>
        ///     Adds a connector to subscribe to a message broker and forward the incoming integration messages to the internal
        ///     bus. This implementation stores the offset of the latest consumed messages and prevents duplicated processing of
        ///     the
        ///     same message.
        /// </summary>
        IBrokerOptionsBuilder AddOffsetStoredInboundConnector(Func<IServiceProvider, IOffsetStore> offsetStoreFactory);

        /// <summary>
        ///     Adds a connector to subscribe to a message broker and forward the incoming integration messages to the internal
        ///     bus. This implementation logs the incoming messages in the database and prevents duplicated processing of the same
        ///     message.
        /// </summary>
        IBrokerOptionsBuilder AddDbLoggedInboundConnector();

        /// <summary>
        ///     Adds a connector to subscribe to a message broker and forward the incoming integration messages to the internal
        ///     bus. This implementation stores the offset of the latest consumed messages in the database and prevents duplicated
        ///     processing of the same message.
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
        IBrokerOptionsBuilder AddDeferredOutboundConnector(
            Func<IServiceProvider, IOutboundQueueProducer> outboundQueueProducerFactory);

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
        /// <param name="outboundQueueConsumerFactory"></param>
        /// <param name="interval">The interval between each run (default is 500ms).</param>
        /// <param name="enforceMessageOrder">
        ///     If set to <c>true</c> the message order will be preserved (no message will be
        ///     skipped).
        /// </param>
        /// <param name="readPackageSize">The number of messages to be loaded from the queue at once.</param>
        IBrokerOptionsBuilder AddOutboundWorker(
            Func<IServiceProvider, IOutboundQueueConsumer> outboundQueueConsumerFactory,
            TimeSpan? interval = null,
            bool enforceMessageOrder = true,
            int readPackageSize = 100);

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
        IBrokerOptionsBuilder AddOutboundWorker(
            Func<IServiceProvider, IOutboundQueueConsumer> outboundQueueConsumerFactory,
            DistributedLockSettings distributedLockSettings,
            TimeSpan? interval = null,
            bool enforceMessageOrder = true,
            int readPackageSize = 100);

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
        IBrokerOptionsBuilder AddDbOutboundWorker(
            DistributedLockSettings distributedLockSettings,
            TimeSpan? interval = null,
            bool enforceMessageOrder = true,
            int readPackageSize = 100,
            bool removeProduced = true);

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
        IBrokerOptionsBuilder AddDbOutboundWorker(
            TimeSpan? interval = null,
            bool enforceMessageOrder = true,
            int readPackageSize = 100,
            bool removeProduced = true);

        #endregion

        #region AddChunkStore

        /// <summary>
        ///     Adds a chunk store to temporary save the message chunks until the full message has been received.
        /// </summary>
        IBrokerOptionsBuilder AddChunkStore<TStore>()
            where TStore : class, IChunkStore;

        /// <summary>
        ///     Adds a chunk store to temporary save the message chunks until the full message has been received.
        ///     This implementation stores the message chunks in the database.
        /// </summary>
        IBrokerOptionsBuilder AddDbChunkStore();

        #endregion

        #region AddMessageIdProvider

        /// <summary>
        ///     Adds a message id provider of type <typeparamref name="TProvider" /> to be used
        ///     to auto-generate the unique id of the integration messages.
        /// </summary>
        /// <typeparam name="TProvider">The type of the <see cref="IMessageIdProvider" /> to add.</typeparam>
        /// <returns></returns>
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
    }
}