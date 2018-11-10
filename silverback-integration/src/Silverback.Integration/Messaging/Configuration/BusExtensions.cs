using System;
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
        public static void ConnectBrokers(this IBus bus)
            => GetBrokers(bus).ForEach(b => b.Connect());


        /// <summary>
        /// Disconnects from the message brokers.
        /// </summary>
        public static void DisconnectBrokers(this IBus bus)
            => GetBrokers(bus).ForEach(b => b.Disconnect());

        #endregion

        #region ConfigureBroker

        /// <summary>
        /// Configures an <see cref="IBroker" /> to be used for inbound/outbound messaging.
        /// </summary>
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
        public static IBus AddOutbound<TMessage>(this IBus bus, IEndpoint endpoint, Func<TMessage, bool> filter = null)
            where TMessage : IIntegrationMessage
            => bus.AddOutbound<TMessage, OutboundConnector>(endpoint, filter);

        // TODO: Test
        /// <summary>
        /// Attaches an <see cref="OutboundConnector" /> to the bus.
        /// </summary>
        public static IBus AddOutbound(this IBus bus, IEndpoint endpoint, Func<IIntegrationMessage, bool> filter = null)
            => bus.AddOutbound<IIntegrationMessage>(endpoint, filter);

        // TODO: Test
        /// <summary>
        /// Attaches a <see cref="DeferredOutboundConnector" /> to the bus.
        /// </summary>
        public static IBus AddDeferredOutbound<TMessage>(this IBus bus, IEndpoint endpoint, Func<TMessage, bool> filter = null)
            where TMessage : IIntegrationMessage
            => bus.AddOutbound<TMessage, DeferredOutboundConnector>(endpoint, filter);

        // TODO: Test
        /// <summary>
        /// Attaches a <see cref="DeferredOutboundConnector" /> to the bus.
        /// </summary>
        public static IBus AddDeferredOutbound(this IBus bus, IEndpoint endpoint, Func<IIntegrationMessage, bool> filter = null)
            => bus.AddDeferredOutbound<IIntegrationMessage>(endpoint, filter);

        #endregion

        #region Inbound

        /// <summary>
        /// Configures the <see cref="IInboundConnector" /> to forward the messages to the internal bus.
        /// </summary>
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
        public static IBus AddInbound(this IBus bus, IEndpoint endpoint, IErrorPolicy errorPolicy = null)
            => bus.AddInbound(new InboundConnector(), endpoint, errorPolicy);

        // TODO: Test
        /// <summary>
        /// Configures a <see cref="LoggedInboundConnector" /> to forward the messages to the internal bus.
        /// </summary>
        public static IBus AddLoggedInbound(this IBus bus, IEndpoint endpoint, IErrorPolicy errorPolicy = null)
            => bus.AddInbound<LoggedInboundConnector>(endpoint, errorPolicy);

        #endregion

        #region Translator

        /// <summary>
        /// Configures a <see cref="MessageTranslator{TSource,TDestination}" />.
        /// </summary>
        /// <returns></returns>
        public static IBus AddTranslator<TSource, TDestination, TTranslator>(this IBus bus)
            where TSource : IMessage
            where TDestination : IMessage
            where TTranslator : MessageTranslator<TSource, TDestination>
        => bus.Subscribe<TTranslator>();

        /// <summary>
        /// Configures a <see cref="MessageTranslator{TSource,TDestination}" />.
        /// </summary>
        public static IBus AddTranslator<TMessage, TIntegrationMessage>(this IBus bus, Func<TMessage, TIntegrationMessage> mapper, Func<TMessage, bool> filter = null)
            where TMessage : IMessage
            where TIntegrationMessage : IIntegrationMessage
        {
            var translator = new GenericMessageTranslator<TMessage, TIntegrationMessage>(mapper);
            return bus.Subscribe<TMessage>(m => translator.OnNext(m, bus));
        }

        #endregion
    }
}