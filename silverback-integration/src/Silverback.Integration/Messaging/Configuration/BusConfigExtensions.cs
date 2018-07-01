using System;
using Silverback.Messaging.Adapters;
using Silverback.Messaging.Broker;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Repositories;
using Silverback.Messaging.Subscribers;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    /// Adds some methods to <see cref="BusConfig"/> to add inbound and outbound adapters.
    /// </summary>
    public static class BusConfigExtensions
    {
        #region Broker

        /// <summary>
        /// Configures an <see cref="IBroker" /> to be used for inbound/outbound messaging.
        /// </summary>
        /// <typeparam name="TBroker">The type of the broker.</typeparam>
        /// <param name="config">The configuration.</param>
        /// <param name="brokerConfig">The method applying the broker configuration.</param>
        /// <returns></returns>
        public static BusConfig ConfigureBroker<TBroker>(this BusConfig config, Action<TBroker> brokerConfig)
            where TBroker : IBroker, new()
        {
            config.Bus.GetBrokers().Add(brokerConfig);
            return config;
        }

        /// <summary>
        /// Connects to the message brokers to start consuming and producing messages.
        /// </summary>
        /// <param name="config">The configuration.</param>
        public static void ConnectBrokers(this BusConfig config)
        {
            config.Bus.ConnectBrokers();
        }

        #endregion

        #region Outbound

        /// <summary>
        /// Attaches the <see cref="IOutboundAdapter" /> to the bus.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <typeparam name="TAdapter">The type of the adapter.</typeparam>
        /// <param name="config">The configuration.</param>
        /// <param name="endpoint">The endpoint to be passed to the <see cref="IOutboundAdapter" />.</param>
        /// <param name="filter">An optional filter to be applied to the messages.</param>
        /// <returns></returns>
        public static BusConfig AddOutbound<TMessage, TAdapter>(this BusConfig config, IEndpoint endpoint, Func<TMessage, bool> filter = null)
            where TMessage : IIntegrationMessage
            where TAdapter : IOutboundAdapter
        {
            config.Bus.Subscribe(new OutboundSubscriber<TMessage, TAdapter>(
                config.GetTypeFactory, config.Bus.GetBroker(endpoint.BrokerName), endpoint));
            return config;
        }

        // TODO: Test
        /// <summary>
        /// Attaches a <see cref="SimpleOutboundAdapter"/> to the bus.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="config">The configuration.</param>
        /// <param name="endpoint">The endpoint to be passed to the <see cref="IOutboundAdapter" />.</param>
        /// <param name="filter">An optional filter to be applied to the messages.</param>
        /// <returns></returns>
        public static BusConfig AddOutbound<TMessage>(this BusConfig config, IEndpoint endpoint, Func<TMessage, bool> filter = null)
            where TMessage : IIntegrationMessage
            => AddOutbound<TMessage, SimpleOutboundAdapter>(config, endpoint, filter);

        // TODO: Test
        /// <summary>
        /// Attaches a <see cref="SimpleOutboundAdapter"/> to the bus.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="endpoint">The endpoint to be passed to the <see cref="IOutboundAdapter" />.</param>
        /// <param name="filter">An optional filter to be applied to the messages.</param>
        /// <returns></returns>
        public static BusConfig AddOutbound(this BusConfig config, IEndpoint endpoint, Func<IIntegrationMessage, bool> filter = null)
            => AddOutbound<IIntegrationMessage>(config, endpoint, filter);

        // TODO: Test
        /// <summary>
        /// Attaches a <see cref="DbOutboundAdapter{TEntity}"/> to the bus.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="config">The configuration.</param>
        /// <param name="endpoint">The endpoint to be passed to the <see cref="IOutboundAdapter" />.</param>
        /// <param name="filter">An optional filter to be applied to the messages.</param>
        /// <returns></returns>
        public static BusConfig AddDbOutbound<TMessage, TEntity>(this BusConfig config, IEndpoint endpoint, Func<TMessage, bool> filter = null)
            where TMessage : IIntegrationMessage
            where TEntity : IOutboundMessageEntity
            => AddOutbound<TMessage, DbOutboundAdapter<TEntity>>(config, endpoint, filter);

        // TODO: Test
        /// <summary>
        /// Attaches a <see cref="DbOutboundAdapter{TEntity}"/> to the bus.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="config">The configuration.</param>
        /// <param name="endpoint">The endpoint to be passed to the <see cref="IOutboundAdapter" />.</param>
        /// <param name="filter">An optional filter to be applied to the messages.</param>
        /// <returns></returns>
        public static BusConfig AddDbOutbound<TEntity>(this BusConfig config, IEndpoint endpoint, Func<IIntegrationMessage, bool> filter = null)
            where TEntity : IOutboundMessageEntity
            => AddDbOutbound<IIntegrationMessage, TEntity>(config, endpoint, filter);


        #endregion

        #region Inbound

        /// <summary>
        /// Configures the <see cref="IInboundAdapter" /> to forward the messages to the internal bus.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="adapter">The adapter.</param>
        /// <param name="endpoint">The endpoint to be passed to the <see cref="IOutboundAdapter" />.</param>
        /// <param name="errorPolicy">An optional error handling policy.</param>
        public static BusConfig AddInbound(this BusConfig config, IInboundAdapter adapter, IEndpoint endpoint, IErrorPolicy errorPolicy = null)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (adapter == null) throw new ArgumentNullException(nameof(adapter));
            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));

            adapter.Init(config.Bus, endpoint, errorPolicy);
            return config;
        }

        // TODO: Test
        /// <summary>
        /// Configures an <see cref="IInboundAdapter" /> of the specified type to forward the messages to the internal bus.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="endpoint">The endpoint to be passed to the <see cref="IOutboundAdapter" />.</param>
        /// <param name="errorPolicy">An optional error handling policy.</param>
        /// <returns></returns>
        public static BusConfig AddInbound<TAdapter>(this BusConfig config, IEndpoint endpoint, IErrorPolicy errorPolicy = null)
            where TAdapter : IInboundAdapter
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));

            var adapter = config.GetTypeFactory().GetInstance<TAdapter>();
            config.Bus.AddInboundAdapterItem(adapter);
            adapter.Init(config.Bus, endpoint, errorPolicy);
            return config;
        }

        // TODO: Test
        /// <summary>
        /// Configures a <see cref="SimpleInboundAdapter" /> to forward the messages to the internal bus.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="endpoint">The endpoint to be passed to the <see cref="IOutboundAdapter" />.</param>
        /// <param name="errorPolicy">An optional error handling policy.</param>
        /// <returns></returns>
        public static BusConfig AddInbound(this BusConfig config, IEndpoint endpoint, IErrorPolicy errorPolicy = null)
            => AddInbound(config, new SimpleInboundAdapter(), endpoint, errorPolicy);

        // TODO: Test
        /// <summary>
        /// Configures a <see cref="DbInboundAdapter{TEntity}" /> to forward the messages to the internal bus.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="config">The configuration.</param>
        /// <param name="endpoint">The endpoint to be passed to the <see cref="IOutboundAdapter" />.</param>
        /// <param name="errorPolicy">An optional error handling policy.</param>
        /// <returns></returns>
        public static BusConfig AddDbInbound<TEntity>(this BusConfig config, IEndpoint endpoint, IErrorPolicy errorPolicy = null)
            where TEntity : IInboundMessageEntity
            => AddInbound<DbInboundAdapter<TEntity>>(config, endpoint, errorPolicy);

        #endregion

        #region Translator

        /// <summary>
        /// Configures a <see cref="MessageTranslator{TMessage, TIntegrationMessage}" />.
        /// </summary>
        /// <typeparam name="TMessage">The type of the messages.</typeparam>
        /// <typeparam name="TIntegrationMessage">The type of the integration message.</typeparam>
        /// <typeparam name="TTranslator">Type of the <see cref="MessageTranslator{TMessage, TIntegrationMessage}" /> to be used to translate the messages.</typeparam>
        /// <param name="config">The configuration.</param>
        /// <returns></returns>
        public static BusConfig AddTranslator<TMessage, TIntegrationMessage, TTranslator>(this BusConfig config)
            where TMessage : IMessage
            where TIntegrationMessage : IIntegrationMessage
            where TTranslator : MessageTranslator<TMessage, TIntegrationMessage>
        {
            config.Subscribe<TTranslator>();
            return config;
        }

        /// <summary>
        /// Configures a <see cref="MessageTranslator{TMessage, TIntegrationMessage}" />.
        /// </summary>
        /// <typeparam name="TMessage">The type of the messages.</typeparam>
        /// <typeparam name="TIntegrationMessage">The type of the integration message.</typeparam>
        /// <param name="config">The configuration.</param>
        /// <param name="mapper">The mapper method.</param>
        /// <param name="filter">An optional filter to be applied to the published messages.</param>
        /// <returns></returns>
        public static BusConfig AddTranslator<TMessage, TIntegrationMessage>(this BusConfig config, Func<TMessage, TIntegrationMessage> mapper, Func<TMessage, bool> filter = null)
            where TMessage : IMessage
            where TIntegrationMessage : IIntegrationMessage
        {
            config.Subscribe(new GenericMessageTranslator<TMessage, TIntegrationMessage>(mapper, config.Bus, filter));
            return config;
        }

        #endregion
    }
}
