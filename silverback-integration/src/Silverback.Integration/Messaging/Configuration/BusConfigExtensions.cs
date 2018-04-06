using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;
using Silverback.Messaging.Adapters;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    /// Adds some methods to <see cref="BusConfig"/> to add inbound and outbound adapters.
    /// </summary>
    public static class BusConfigExtensions
    {
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
                messages.OfType<TMessage>(), config.TypeFactory, adapterType, endpoint));
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
                messages.OfType<TMessage>(), config.TypeFactory, typeof(TAdapter), endpoint));
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
            adapter.Init(config.Bus, endpoint);
            return config;
        }

        #endregion

        #region AddTranslator

        /// <summary>
        /// Configures a <see cref="MessageTranslator{TMessage, TIntegrationMessage}" />.
        /// </summary>
        /// <typeparam name="TMessage">The type of the messages.</typeparam>
        /// <typeparam name="TIntegrationMessage">The type of the integration message.</typeparam>
        /// <typeparam name="THandler">Type of the <see cref="IMessageHandler{TMessage}" /> to be used to handle the messages.</typeparam>
        /// <param name="config">The configuration.</param>
        /// <param name="filter">An optional filter to be applied to the published messages.</param>
        /// <returns></returns>
        public static BusConfig AddTranslator<TMessage, TIntegrationMessage, THandler>(this BusConfig config, Func<TMessage, bool> filter = null)
            where TMessage : IMessage
            where TIntegrationMessage : IIntegrationMessage
            where THandler : MessageTranslator<TMessage, TIntegrationMessage>
        {
            config.Bus.Subscribe(messages => new DefaultSubscriber<TMessage>(
                messages.OfType<TMessage>(), config.TypeFactory, typeof(THandler), filter));
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
            config.Bus.Subscribe(messages => new DefaultSubscriber<TMessage>(
                messages.OfType<TMessage>(),
                new GenericTypeFactory(_ => new GenericMessageTranslator<TMessage, TIntegrationMessage>(mapper, config.Bus)),
                typeof(GenericMessageHandler<IMessage>),
                filter));
            return config;
        }

        #endregion
    }
}
