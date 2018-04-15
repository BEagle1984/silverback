using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;
using Silverback.Messaging.Adapters;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    /// Adds some methods to <see cref="BusConfig"/> to add inbound and outbound adapters.
    /// </summary>
    public static class BusConfigExtensions
    {
        #region ConfigureBroker

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

        #endregion

        #region AddOutbound

        /// <summary>
        /// Attaches the <see cref="IOutboundAdapter"/> to the bus.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="config">The configuration.</param>
        /// <param name="adapterType">The type of the adapter to be instanciated to relay the messages.</param>
        /// <param name="endpoint">The endpoint to be passed to the <see cref="IOutboundAdapter"/>.</param>
        /// <returns></returns>
        public static BusConfig AddOutbound<TMessage>(this BusConfig config, Type adapterType, IEndpoint endpoint)
        where TMessage : IIntegrationMessage
        {
            config.Bus.Subscribe(messages => new OutboundSubscriber<TMessage>(
                messages, config.TypeFactory, adapterType, config.Bus.GetBroker(endpoint.BrokerName), endpoint));
            return config;
        }

        /// <summary>
        /// Attaches the <see cref="IOutboundAdapter" /> to the bus.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <typeparam name="TAdapter">The type of the adapter.</typeparam>
        /// <param name="config">The configuration.</param>
        /// <param name="endpoint">The endpoint to be passed to the <see cref="IOutboundAdapter" />.</param>
        /// <returns></returns>
        public static BusConfig AddOutbound<TMessage, TAdapter>(this BusConfig config, IEndpoint endpoint)
            where TMessage : IIntegrationMessage
            where TAdapter : IOutboundAdapter
        {
            config.Bus.Subscribe(messages => new OutboundSubscriber<TMessage>(
                messages, config.TypeFactory, typeof(TAdapter), config.Bus.GetBroker(endpoint.BrokerName), endpoint));
            return config;
        }

        /// <summary>
        /// Attaches the <see cref="IOutboundAdapter"/> to the bus.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="adapterType">The type of the adapter to be instanciated to relay the messages.</param>
        /// <param name="endpoint">The endpoint to be passed to the <see cref="IOutboundAdapter"/>.</param>
        /// <returns></returns>
        public static BusConfig AddOutbound(this BusConfig config, Type adapterType, IEndpoint endpoint)
            => AddOutbound<IIntegrationMessage>(config, adapterType, endpoint);

        /// <summary>
        /// Attaches the <see cref="IOutboundAdapter" /> to the bus.
        /// </summary>
        /// <typeparam name="TAdapter">The type of the adapter.</typeparam>
        /// <param name="config">The configuration.</param>
        /// <param name="endpoint">The endpoint to be passed to the <see cref="IOutboundAdapter" />.</param>
        /// <returns></returns>
        public static BusConfig AddOutbound<TAdapter>(this BusConfig config, IEndpoint endpoint)
            where TAdapter : IOutboundAdapter
            => AddOutbound<IIntegrationMessage, TAdapter>(config, endpoint);

        #endregion

        #region AddInbound

        /// <summary>
        /// Configures the <see cref="IInboundAdapter" /> to forward the messages to the internal bus.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="adapter">The adapter.</param>
        /// <param name="endpoint">The endpoint to be passed to the <see cref="IOutboundAdapter" />.</param>
        /// <returns></returns>
        public static BusConfig AddInbound(this BusConfig config, IInboundAdapter adapter, IEndpoint endpoint)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (adapter == null) throw new ArgumentNullException(nameof(adapter));
            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));

            adapter.Init(config.Bus, config.Bus.GetBroker(endpoint.BrokerName), endpoint);
            return config;
        }

        #endregion

        #region AddTranslator

        /// <summary>
        /// Configures a <see cref="MessageTranslator{TMessage, TIntegrationMessage}" />.
        /// </summary>
        /// <typeparam name="TMessage">The type of the messages.</typeparam>
        /// <typeparam name="TIntegrationMessage">The type of the integration message.</typeparam>
        /// <typeparam name="THandler">Type of the <see cref="IMessageHandler" /> to be used to handle the messages.</typeparam>
        /// <param name="config">The configuration.</param>
        /// <returns></returns>
        public static BusConfig AddTranslator<TMessage, TIntegrationMessage, THandler>(this BusConfig config)
            where TMessage : IMessage
            where TIntegrationMessage : IIntegrationMessage
            where THandler : MessageTranslator<TMessage, TIntegrationMessage>
        {
            config.Bus.Subscribe(messages => new DefaultSubscriber(
                messages, config.TypeFactory, typeof(THandler)));
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
            config.Bus.Subscribe(messages => new DefaultSubscriber(
                messages,
                new GenericTypeFactory(_ => new GenericMessageTranslator<TMessage, TIntegrationMessage>(mapper, config.Bus, filter)),
                typeof(IMessageHandler)));
            return config;
        }

        #endregion
    }
}
