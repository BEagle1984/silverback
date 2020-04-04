// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.ErrorHandling;

namespace Silverback.Messaging.Configuration
{
    public interface IEndpointsConfigurationBuilder
    {
        /// <summary>
        ///     Adds and outbound endpoint for the specified message type.
        /// </summary>
        /// <param name="endpoints">The endpoints (topics).</param>
        /// <typeparam name="TMessage">The type of the messages to be published to this endpoint.</typeparam>
        /// <typeparam name="TConnector">The type of the <see cref="IOutboundConnector" /> to be used.</typeparam>
        IEndpointsConfigurationBuilder AddOutbound<TMessage, TConnector>(params IProducerEndpoint[] endpoints)
            where TConnector : IOutboundConnector;

        /// <summary>
        ///     Adds and outbound endpoint for the specified message type.
        /// </summary>
        /// <param name="endpoints">The endpoints (topics).</param>
        /// <typeparam name="TMessage">The type of the messages to be published to this endpoint.</typeparam>
        /// <typeparam name="TConnector">The type of the <see cref="IOutboundConnector" /> to be used.</typeparam>
        IEndpointsConfigurationBuilder AddOutbound<TMessage, TConnector>(IEnumerable<IProducerEndpoint> endpoints)
            where TConnector : IOutboundConnector;

        /// <summary>
        ///     Adds and outbound endpoint for the specified message type.
        /// </summary>
        /// <param name="endpoints">The endpoints (topics).</param>
        /// <typeparam name="TMessage">The type of the messages to be published to this endpoint.</typeparam>
        IEndpointsConfigurationBuilder AddOutbound<TMessage>(params IProducerEndpoint[] endpoints);

        /// <summary>
        ///     Adds and outbound endpoint for the specified message type.
        /// </summary>
        /// <param name="endpoints">The endpoints (topics).</param>
        /// <param name="outboundConnectorType">
        ///     The type of the <see cref="IOutboundConnector" /> to be used.
        ///     If not specified, the default one will be used.
        /// </param>
        /// <typeparam name="TMessage">The type of the messages to be published to this endpoint.</typeparam>
        IEndpointsConfigurationBuilder AddOutbound<TMessage>(
            IEnumerable<IProducerEndpoint> endpoints,
            Type outboundConnectorType = null);

        /// <summary>
        ///     Adds and outbound endpoint for the specified message type.
        /// </summary>
        /// <param name="messageType">The type of the messages to be published to this endpoint.</param>
        /// <param name="endpoints">The endpoints (topics).</param>
        /// <param name="outboundConnectorType">
        ///     The type of the <see cref="IOutboundConnector" /> to be used.
        ///     If not specified, the default one will be used.
        /// </param>
        IEndpointsConfigurationBuilder AddOutbound(
            Type messageType,
            IEnumerable<IProducerEndpoint> endpoints,
            Type outboundConnectorType);

        /// <summary>
        ///     Adds and outbound endpoint for the specified message type.
        /// </summary>
        /// <typeparam name="TMessage">The type of the messages to be published to this endpoint.</typeparam>
        /// <typeparam name="TRouter">
        ///     The type of the <see cref="IOutboundRouter{TMessage}" /> to be used to determine the
        ///     destination endpoint.
        /// </typeparam>
        /// <typeparam name="TConnector">The type of the <see cref="IOutboundConnector" /> to be used.</typeparam>
        IEndpointsConfigurationBuilder AddOutbound<TMessage, TRouter, TConnector>()
            where TRouter : IOutboundRouter<TMessage>
            where TConnector : IOutboundConnector;

        /// <summary>
        ///     Adds and outbound endpoint for the specified message type.
        /// </summary>
        /// <typeparam name="TMessage">The type of the messages to be published to this endpoint.</typeparam>
        /// <typeparam name="TRouter">
        ///     The type of the <see cref="IOutboundRouter{TMessage}" /> to be used to determine the
        ///     destination endpoint.
        /// </typeparam>
        IEndpointsConfigurationBuilder AddOutbound<TMessage, TRouter>()
            where TRouter : IOutboundRouter<TMessage>;

        /// <summary>
        ///     Adds and outbound endpoint for the specified message type.
        /// </summary>
        /// <param name="router">The <see cref="IOutboundRouter{TMessage}" /> to be used to determine the destination endpoint.</param>
        /// <typeparam name="TMessage">The type of the messages to be published to this endpoint.</typeparam>
        /// <typeparam name="TConnector">The type of the <see cref="IOutboundConnector" /> to be used.</typeparam>
        IEndpointsConfigurationBuilder AddOutbound<TMessage, TConnector>(IOutboundRouter<TMessage> router)
            where TConnector : IOutboundConnector;

        /// <summary>
        ///     Adds and outbound endpoint for the specified message type.
        /// </summary>
        /// <typeparam name="TMessage">The type of the messages to be published to this endpoint.</typeparam>
        /// <param name="router">The <see cref="IOutboundRouter{TMessage}" /> to be used to determine the destination endpoint.</param>
        /// <param name="outboundConnectorType">The type of the <see cref="IOutboundConnector" /> to be used.</param>
        IEndpointsConfigurationBuilder AddOutbound<TMessage>(
            IOutboundRouter<TMessage> router,
            Type outboundConnectorType = null);


        /// <summary>
        ///     Adds and outbound endpoint for the specified message type.
        /// </summary>
        /// <param name="messageType">The type of the messages to be published to this endpoint.</param>
        /// <param name="routerType">
        ///     The type of the <see cref="IOutboundRouter{TMessage}" /> to be used to determine the
        ///     destination endpoint.
        /// </param>
        /// <param name="outboundConnectorType">The type of the <see cref="IOutboundConnector" /> to be used.</param>
        IEndpointsConfigurationBuilder AddOutbound(
            Type messageType,
            Type routerType,
            Type outboundConnectorType);

        /// <summary>
        ///     Adds and outbound endpoint for the specified message type.
        /// </summary>
        /// <param name="messageType">The type of the messages to be published to this endpoint.</param>
        /// <param name="router">The <see cref="IOutboundRouter{TMessage}" /> to be used to determine the destination endpoint.</param>
        /// <param name="outboundConnectorType">The type of the <see cref="IOutboundConnector" /> to be used.</param>
        IEndpointsConfigurationBuilder AddOutbound(
            Type messageType,
            IOutboundRouter router,
            Type outboundConnectorType);

        /// <summary>
        ///     Adds and inbound endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint (topic).</param>
        /// <param name="errorPolicyFactory">
        ///     An optional function returning the error policy to be applied in case of
        ///     exceptions while consuming the messages from this topic.
        /// </param>
        /// <param name="settings">The optional additional settings. If not specified, the default settings will be used.</param>
        IEndpointsConfigurationBuilder AddInbound(
            IConsumerEndpoint endpoint,
            Func<ErrorPolicyBuilder, IErrorPolicy> errorPolicyFactory = null,
            InboundConnectorSettings settings = null);

        /// <summary>
        ///     Adds and inbound endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint (topic).</param>
        /// <param name="errorPolicyFactory">
        ///     An optional function returning the error policy to be applied in case of
        ///     exceptions while consuming the messages from this topic.
        /// </param>
        /// <param name="settings">The optional additional settings. If not specified, the default settings will be used.</param>
        /// <typeparam name="TConnector">The type of the <see cref="IInboundConnector" /> to be used.</typeparam>
        IEndpointsConfigurationBuilder AddInbound<TConnector>(
            IConsumerEndpoint endpoint,
            Func<ErrorPolicyBuilder, IErrorPolicy> errorPolicyFactory = null,
            InboundConnectorSettings settings = null)
            where TConnector : IInboundConnector;

        /// <summary>
        ///     Adds and inbound endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint (topic).</param>
        /// <param name="inboundConnectorType">
        ///     The type of the <see cref="IInboundConnector" /> to be used.
        ///     If not specified, the default one will be used.
        /// </param>
        /// <param name="errorPolicyFactory">
        ///     An optional function returning the error policy to be applied in case of
        ///     exceptions while consuming the messages from this topic.
        /// </param>
        /// <param name="settings">The optional additional settings. If not specified, the default settings will be used.</param>
        IEndpointsConfigurationBuilder AddInbound(
            IConsumerEndpoint endpoint,
            Type inboundConnectorType,
            Func<ErrorPolicyBuilder, IErrorPolicy> errorPolicyFactory = null,
            InboundConnectorSettings settings = null);

        /// <summary>
        ///     Enables the legacy behavior where the messages to be routed through an outbound connector are also being
        ///     published to the internal bus, to be locally subscribed. This is now disabled by default.
        /// </summary>
        IEndpointsConfigurationBuilder PublishOutboundMessagesToInternalBus();
    }
}