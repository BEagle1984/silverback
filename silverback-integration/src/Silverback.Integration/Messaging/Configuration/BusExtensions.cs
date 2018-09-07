using System;
using System.Linq;
using Silverback.Messaging.Broker;
using Silverback.Extensions;
using Silverback.Messaging.Adapters;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Repositories;
using Silverback.Messaging.Subscribers;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    /// Contains a set of extension methods useful to setup the <see cref="IBus"/> 
    /// configuring the brokers and adding inbound/outbound adapters.
    /// </summary>
    public static class BusExtensions
    {
        private const string ItemsKeyPrefix = "Silverback.Integration.Configuration.";

        #region Brokers Collection

        /// <summary>
        /// Gets the list of <see cref="IEndpoint"/> configured as inbound.
        /// </summary>
        /// <param name="bus">The bus.</param>
        /// <returns></returns>
        internal static BrokersCollection GetBrokers(this IBus bus)
            => (BrokersCollection)bus.Items.GetOrAdd(ItemsKeyPrefix + "Brokers", _ => new BrokersCollection());

        /// <summary>
        /// Gets the <see cref="T:Silverback.Messaging.Broker.IBroker" /> associated with the <see cref="IBus"/>.
        /// </summary>
        /// <param name="bus">The bus.</param>
        /// <param name="name">The name of the broker. If not set the default one will be returned.</param>
        /// <returns></returns>
        internal static IBroker GetBroker(this IBus bus, string name = null)
            => GetBrokers(bus).Get(name);

        /// <summary>
        /// Gets the <see cref="T:Silverback.Messaging.Broker.IBroker" /> associated with the <see cref="IBus"/>.
        /// </summary>
        /// <typeparam name="TBroker">The type of the broker.</typeparam>
        /// <param name="bus">The bus.</param>
        /// <param name="name">The name of the broker. If not set the default one will be returned.</param>
        /// <returns></returns>
        internal static TBroker GetBroker<TBroker>(this IBus bus, string name = null) where TBroker : IBroker
            => (TBroker)GetBroker(bus, name);

        #endregion

        #region Inbound Adapters

        /// <summary>
        /// Gets the item with of the specified .
        /// </summary>
        /// <param name="bus">The bus.</param>
        /// <param name="adapter">The adapter.</param>
        internal static void AddInboundAdapterItem(this IBus bus, IInboundAdapter adapter)
        {
            if (!bus.Items.TryAdd($"{ItemsKeyPrefix}InboundAdapter.{adapter.GetType().Name}.{Guid.NewGuid():N}", adapter))
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
        public static IBus ConfigureBroker<TBroker>(this IBus bus, Action<TBroker> brokerConfig)
            where TBroker : IBroker, new()
        {
            bus.GetBrokers().Add(brokerConfig);
            return bus;
        }

        #endregion

        #region Outbound

        /// <summary>
        /// Attaches the <see cref="IOutboundAdapter" /> to the bus.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <typeparam name="TAdapter">The type of the adapter.</typeparam>
        /// <param name="bus">The bus.</param>
        /// <param name="endpoint">The endpoint to be passed to the <see cref="IOutboundAdapter" />.</param>
        /// <param name="filter">An optional filter to be applied to the messages.</param>
        /// <returns></returns>
        public static IBus AddOutbound<TMessage, TAdapter>(this IBus bus, IEndpoint endpoint, Func<TMessage, bool> filter = null)
            where TMessage : IIntegrationMessage
            where TAdapter : IOutboundAdapter
        {
            bus.Subscribe(new OutboundSubscriber<TMessage, TAdapter>(bus.GetTypeFactory(), bus.GetBroker(endpoint.BrokerName), endpoint));
            return bus;
        }

        // TODO: Test
        /// <summary>
        /// Attaches a <see cref="SimpleOutboundAdapter" /> to the bus.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="bus">The bus.</param>
        /// <param name="endpoint">The endpoint to be passed to the <see cref="IOutboundAdapter" />.</param>
        /// <param name="filter">An optional filter to be applied to the messages.</param>
        /// <returns></returns>
        public static IBus AddOutbound<TMessage>(this IBus bus, IEndpoint endpoint, Func<TMessage, bool> filter = null)
            where TMessage : IIntegrationMessage
            => bus.AddOutbound<TMessage, SimpleOutboundAdapter>(endpoint, filter);

        // TODO: Test
        /// <summary>
        /// Attaches a <see cref="SimpleOutboundAdapter" /> to the bus.
        /// </summary>
        /// <param name="bus">The bus.</param>
        /// <param name="endpoint">The endpoint to be passed to the <see cref="IOutboundAdapter" />.</param>
        /// <param name="filter">An optional filter to be applied to the messages.</param>
        /// <returns></returns>
        public static IBus AddOutbound(this IBus bus, IEndpoint endpoint, Func<IIntegrationMessage, bool> filter = null)
            => bus.AddOutbound<IIntegrationMessage>(endpoint, filter);

        // TODO: Test
        /// <summary>
        /// Attaches a <see cref="DbOutboundAdapter{TEntity}" /> to the bus.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="bus">The bus.</param>
        /// <param name="endpoint">The endpoint to be passed to the <see cref="IOutboundAdapter" />.</param>
        /// <param name="filter">An optional filter to be applied to the messages.</param>
        /// <returns></returns>
        public static IBus AddDbOutbound<TMessage, TEntity>(this IBus bus, IEndpoint endpoint, Func<TMessage, bool> filter = null)
            where TMessage : IIntegrationMessage
            where TEntity : IOutboundMessageEntity
            => bus.AddOutbound<TMessage, DbOutboundAdapter<TEntity>>(endpoint, filter);

        // TODO: Test
        /// <summary>
        /// Attaches a <see cref="DbOutboundAdapter{TEntity}" /> to the bus.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="bus">The bus.</param>
        /// <param name="endpoint">The endpoint to be passed to the <see cref="IOutboundAdapter" />.</param>
        /// <param name="filter">An optional filter to be applied to the messages.</param>
        /// <returns></returns>
        public static IBus AddDbOutbound<TEntity>(this IBus bus, IEndpoint endpoint, Func<IIntegrationMessage, bool> filter = null)
            where TEntity : IOutboundMessageEntity
            => bus.AddDbOutbound<IIntegrationMessage, TEntity>(endpoint, filter);

        #endregion

        #region Inbound

        /// <summary>
        /// Configures the <see cref="IInboundAdapter" /> to forward the messages to the internal bus.
        /// </summary>
        /// <param name="bus">The bus.</param>
        /// <param name="adapter">The adapter.</param>
        /// <param name="endpoint">The endpoint to be passed to the <see cref="IOutboundAdapter" />.</param>
        /// <param name="errorPolicy">An optional error handling policy.</param>
        /// <returns></returns>
        public static IBus AddInbound(this IBus bus, IInboundAdapter adapter, IEndpoint endpoint, IErrorPolicy errorPolicy = null)
        {
            if (bus == null) throw new ArgumentNullException(nameof(bus));
            if (adapter == null) throw new ArgumentNullException(nameof(adapter));
            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));

            adapter.Init(bus, endpoint, errorPolicy);
            return bus;
        }

        // TODO: Test
        /// <summary>
        /// Configures an <see cref="IInboundAdapter" /> of the specified type to forward the messages to the internal bus.
        /// </summary>
        /// <typeparam name="TAdapter">The type of the adapter.</typeparam>
        /// <param name="bus">The bus.</param>
        /// <param name="endpoint">The endpoint to be passed to the <see cref="IOutboundAdapter" />.</param>
        /// <param name="errorPolicy">An optional error handling policy.</param>
        /// <returns></returns>
        public static IBus AddInbound<TAdapter>(this IBus bus, IEndpoint endpoint, IErrorPolicy errorPolicy = null)
            where TAdapter : IInboundAdapter
        {
            if (bus == null) throw new ArgumentNullException(nameof(bus));
            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));

            var adapter = bus.GetTypeFactory().GetInstance<TAdapter>();
            bus.AddInboundAdapterItem(adapter);
            adapter.Init(bus, endpoint, errorPolicy);
            return bus;
        }

        // TODO: Test
        /// <summary>
        /// Configures a <see cref="SimpleInboundAdapter" /> to forward the messages to the internal bus.
        /// </summary>
        /// <param name="bus">The bus.</param>
        /// <param name="endpoint">The endpoint to be passed to the <see cref="IOutboundAdapter" />.</param>
        /// <param name="errorPolicy">An optional error handling policy.</param>
        /// <returns></returns>
        public static IBus AddInbound(this IBus bus, IEndpoint endpoint, IErrorPolicy errorPolicy = null)
            => bus.AddInbound(new SimpleInboundAdapter(), endpoint, errorPolicy);

        // TODO: Test
        /// <summary>
        /// Configures a <see cref="DbInboundAdapter{TEntity}" /> to forward the messages to the internal bus.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="bus">The bus.</param>
        /// <param name="endpoint">The endpoint to be passed to the <see cref="IOutboundAdapter" />.</param>
        /// <param name="errorPolicy">An optional error handling policy.</param>
        /// <returns></returns>
        public static IBus AddDbInbound<TEntity>(this IBus bus, IEndpoint endpoint, IErrorPolicy errorPolicy = null)
            where TEntity : IInboundMessageEntity
            => bus.AddInbound<DbInboundAdapter<TEntity>>(endpoint, errorPolicy);

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
        public static IBus AddMapper<TMessage, TIntegrationMessage, TMapper>(this IBus bus)
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
        public static IBus AddMapper<TMessage, TIntegrationMessage>(this IBus bus, Func<TMessage, TIntegrationMessage> mapper, Func<TMessage, bool> filter = null)
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