using System;
using System.Linq;
using Silverback.Messaging.Broker;
using Silverback.Util;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Integration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    /// Contains a set of extension methods useful to setup the <see cref="IBus"/> 
    /// configuring the brokers and adding inbound/outbound connectors.
    /// </summary>
    public static class BusExtensions
    {
        private const string ItemsKeyPrefix = "Silverback.Integration.Configuration.";

        #region Brokers Collection

        internal static BrokersCollection GetBrokers(this IBus bus)
            => (BrokersCollection)bus.Items.GetOrAdd(ItemsKeyPrefix + "Brokers", _ => new BrokersCollection());

        internal static IBroker GetBroker(this IBus bus, string name = null)
            => GetBrokers(bus).Get(name);

        internal static TBroker GetBroker<TBroker>(this IBus bus, string name = null) where TBroker : IBroker
            => (TBroker)GetBroker(bus, name);

        #endregion

        #region Inbound Connectors

        /// <summary>
        /// Binds the connector lifecycle to the bus so that it stays alive until the bus is disposed.
        /// </summary>
        internal static void BindConnectorLifecycle(this IBus bus, IInboundConnector connector)
        {
            if (!bus.Items.TryAdd($"{ItemsKeyPrefix}InboundConnector.{connector.GetType().Name}.{Guid.NewGuid():N}", connector))
            {
                throw new InvalidOperationException();
            }
        }

        #endregion

        #region Connect

        /// <summary>
        /// Connects to the message brokers to start consuming and producing messages.
        /// </summary>
        /// <param name="bus">The bus.</param>
        public static void ConnectBrokers(this IBus bus)
            => GetBrokers(bus).ForEach(b => b.Connect());


        /// <summary>
        /// Disconnects from the message brokers.
        /// </summary>
        /// <param name="bus">The bus.</param>
        public static void DisconnectBrokers(this IBus bus)
            => GetBrokers(bus).ForEach(b => b.Disconnect());

        #endregion

        #region ConfigureBroker

        /// <summary>
        /// Configures an <see cref="IBroker" /> to be used for inbound/outbound messaging.
        /// </summary>
        /// <typeparam name="TBroker">The type of the broker.</typeparam>
        /// <param name="bus">The bus.</param>
        /// <param name="brokerConfig">The method applying the broker configuration.</param>
        /// <returns></returns>
        public static IBus ConfigureBroker<TBroker>(this IBus bus, Action<TBroker> brokerConfig = null)
            where TBroker : IBroker, new()
        {
            bus.GetBrokers().Add(brokerConfig);
            return bus;
        }

        #endregion

        #region Outbound

        /// <summary>
        /// Attaches the <see cref="IOutboundConnector" /> to the bus.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <typeparam name="TConnector">The type of the connector.</typeparam>
        /// <param name="bus">The bus.</param>
        /// <param name="endpoint">The endpoint to be passed to the <see cref="IOutboundConnector" />.</param>
        /// <param name="filter">An optional filter to be applied to the messages.</param>
        /// <returns></returns>
        public static IBus AddOutbound<TMessage, TConnector>(this IBus bus, IEndpoint endpoint, Func<TMessage, bool> filter = null)
            where TMessage : IIntegrationMessage
            where TConnector : IOutboundConnector
        {
            bus.Subscribe(new OutboundSubscriber<TMessage, TConnector>(bus.GetTypeFactory(), bus.GetBroker(endpoint.BrokerName), endpoint, filter));
            return bus;
        }

        // TODO: Test
        /// <summary>
        /// Attaches an <see cref="OutboundConnector" /> to the bus.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="bus">The bus.</param>
        /// <param name="endpoint">The endpoint to be passed to the <see cref="IOutboundConnector" />.</param>
        /// <param name="filter">An optional filter to be applied to the messages.</param>
        /// <returns></returns>
        public static IBus AddOutbound<TMessage>(this IBus bus, IEndpoint endpoint, Func<TMessage, bool> filter = null)
            where TMessage : IIntegrationMessage
            => bus.AddOutbound<TMessage, OutboundConnector>(endpoint, filter);

        // TODO: Test
        /// <summary>
        /// Attaches an <see cref="OutboundConnector" /> to the bus.
        /// </summary>
        /// <param name="bus">The bus.</param>
        /// <param name="endpoint">The endpoint to be passed to the <see cref="IOutboundConnector" />.</param>
        /// <param name="filter">An optional filter to be applied to the messages.</param>
        /// <returns></returns>
        public static IBus AddOutbound(this IBus bus, IEndpoint endpoint, Func<IIntegrationMessage, bool> filter = null)
            => bus.AddOutbound<IIntegrationMessage>(endpoint, filter);

        // TODO: Test
        /// <summary>
        /// Attaches a <see cref="DeferredOutboundConnector" /> to the bus.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="bus">The bus.</param>
        /// <param name="endpoint">The endpoint to be passed to the <see cref="IOutboundConnector" />.</param>
        /// <param name="filter">An optional filter to be applied to the messages.</param>
        /// <returns></returns>
        public static IBus AddDeferredOutbound<TMessage>(this IBus bus, IEndpoint endpoint, Func<TMessage, bool> filter = null)
            where TMessage : IIntegrationMessage
            => bus.AddOutbound<TMessage, DeferredOutboundConnector>(endpoint, filter);

        // TODO: Test
        /// <summary>
        /// Attaches a <see cref="DeferredOutboundConnector" /> to the bus.
        /// </summary>
        /// <param name="bus">The bus.</param>
        /// <param name="endpoint">The endpoint to be passed to the <see cref="IOutboundConnector" />.</param>
        /// <param name="filter">An optional filter to be applied to the messages.</param>
        /// <returns></returns>
        public static IBus AddDeferredOutbound(this IBus bus, IEndpoint endpoint, Func<IIntegrationMessage, bool> filter = null)
            => bus.AddDeferredOutbound<IIntegrationMessage>(endpoint, filter);

        #endregion

        #region Inbound

        /// <summary>
        /// Configures the <see cref="IInboundConnector" /> to forward the messages to the internal bus.
        /// </summary>
        /// <param name="bus">The bus.</param>
        /// <param name="connector">The connector.</param>
        /// <param name="endpoint">The endpoint to be passed to the <see cref="IOutboundConnector" />.</param>
        /// <param name="errorPolicy">An optional error handling policy.</param>
        /// <returns></returns>
        public static IBus AddInbound(this IBus bus, IInboundConnector connector, IEndpoint endpoint, IErrorPolicy errorPolicy = null)
        {
            if (bus == null) throw new ArgumentNullException(nameof(bus));
            if (connector == null) throw new ArgumentNullException(nameof(connector));
            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));

            connector.Init(bus, endpoint, errorPolicy);
            return bus;
        }

        // TODO: Test
        /// <summary>
        /// Configures an <see cref="IInboundConnector" /> of the specified type to forward the messages to the internal bus.
        /// </summary>
        /// <typeparam name="TConnector">The type of the connector.</typeparam>
        /// <param name="bus">The bus.</param>
        /// <param name="endpoint">The endpoint to be passed to the <see cref="IOutboundConnector" />.</param>
        /// <param name="errorPolicy">An optional error handling policy.</param>
        /// <returns></returns>
        public static IBus AddInbound<TConnector>(this IBus bus, IEndpoint endpoint, IErrorPolicy errorPolicy = null)
            where TConnector : IInboundConnector
        {
            if (bus == null) throw new ArgumentNullException(nameof(bus));
            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));

            // TODO: Move get instance in ConnectBroker() (here the DI could be not ready)
            var connector = bus.GetTypeFactory().GetInstance<TConnector>();
            bus.BindConnectorLifecycle(connector);
            connector.Init(bus, endpoint, errorPolicy);
            return bus;
        }

        // TODO: Test
        /// <summary>
        /// Configures a <see cref="InboundConnector" /> to forward the messages to the internal bus.
        /// </summary>
        /// <param name="bus">The bus.</param>
        /// <param name="endpoint">The endpoint to be passed to the <see cref="IOutboundConnector" />.</param>
        /// <param name="errorPolicy">An optional error handling policy.</param>
        /// <returns></returns>
        public static IBus AddInbound(this IBus bus, IEndpoint endpoint, IErrorPolicy errorPolicy = null)
            => bus.AddInbound(new InboundConnector(), endpoint, errorPolicy);

        // TODO: Test
        /// <summary>
        /// Configures a <see cref="LoggedInboundConnector" /> to forward the messages to the internal bus.
        /// </summary>
        /// <param name="bus">The bus.</param>
        /// <param name="endpoint">The endpoint to be passed to the <see cref="IOutboundConnector" />.</param>
        /// <param name="errorPolicy">An optional error handling policy.</param>
        /// <returns></returns>
        public static IBus AddLoggedInbound(this IBus bus, IEndpoint endpoint, IErrorPolicy errorPolicy = null)
            => bus.AddInbound<LoggedInboundConnector>(endpoint, errorPolicy);

        #endregion

        #region Mapper

        /// <summary>
        /// Configures a <see cref="MessageMapper{TMessage,TIntegrationMessage}" />.
        /// </summary>
        /// <typeparam name="TMessage">The type of the messages.</typeparam>
        /// <typeparam name="TIntegrationMessage">The type of the integration message.</typeparam>
        /// <typeparam name="TMapper">Type of the <see cref="MessageMapper{TMessage,TIntegrationMessage}" /> to be used to translate the messages.</typeparam>
        /// <param name="bus">The bus.</param>
        /// <returns></returns>
        public static IBus AddTranslator<TMessage, TIntegrationMessage, TMapper>(this IBus bus)
            where TMessage : IMessage
            where TIntegrationMessage : IIntegrationMessage
            where TMapper : MessageMapper<TMessage, TIntegrationMessage>
        {
            if (bus == null) throw new ArgumentNullException(nameof(bus));

            var mapper = bus.GetTypeFactory().GetInstance<TMapper>();
            mapper.Init(bus);

            return bus.Subscribe(mapper);
        }

        /// <summary>
        /// Configures a <see cref="MessageMapper{TMessage,TIntegrationMessage}" />.
        /// </summary>
        /// <typeparam name="TMessage">The type of the messages.</typeparam>
        /// <typeparam name="TIntegrationMessage">The type of the integration message.</typeparam>
        /// <param name="bus">The bus.</param>
        /// <param name="mapper">The mapper method.</param>
        /// <param name="filter">An optional filter to be applied to the published messages.</param>
        /// <returns></returns>
        public static IBus AddTranslator<TMessage, TIntegrationMessage>(this IBus bus, Func<TMessage, TIntegrationMessage> mapper, Func<TMessage, bool> filter = null)
            where TMessage : IMessage
            where TIntegrationMessage : IIntegrationMessage
        {
            if (bus == null) throw new ArgumentNullException(nameof(bus));

            var genericMapper = new GenericMessageMapper<TMessage, TIntegrationMessage>(mapper, filter);
            genericMapper.Init(bus);

            return bus.Subscribe(genericMapper);
        }

        #endregion
    }
}