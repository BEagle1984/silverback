// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.ErrorHandling;

namespace Silverback.Messaging.Configuration
{
    public interface IEndpointsConfigurationBuilder
    {
        /// <summary>
        /// Adds and outbound endpoint for the specified message type.
        /// </summary>
        /// <param name="endpoint">The endpoint (topic).</param>
        /// <typeparam name="TMessage">The type of the messages to be published to this endpoint.</typeparam>
        /// <typeparam name="TConnector">The type of the <see cref="IOutboundConnector"/> to be used.</typeparam>
        /// <returns></returns>
        IEndpointsConfigurationBuilder AddOutbound<TMessage, TConnector>(IEndpoint endpoint)
            where TConnector : IOutboundConnector;

        /// <summary>
        /// Adds and outbound endpoint for the specified message type.
        /// </summary>
        /// <param name="endpoint">The endpoint (topic).</param>
        /// <param name="outboundConnectorType">The type of the <see cref="IOutboundConnector"/> to be used.
        /// If not specified, the default one will be used.</param>
        /// <typeparam name="TMessage">The type of the messages to be published to this endpoint.</typeparam>
        /// <returns></returns>
        IEndpointsConfigurationBuilder AddOutbound<TMessage>(IEndpoint endpoint, Type outboundConnectorType = null);

        IEndpointsConfigurationBuilder AddOutbound(Type messageType, IEndpoint endpoint, Type outboundConnectorType);

        /// <summary>
        /// Adds and inbound endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint (topic).</param>
        /// <param name="errorPolicyFactory">An optional function returning the error policy to be applied in case of
        /// exceptions while consuming the messages from this topic.</param>
        /// <param name="settings">The optional additional settings. If not specified, the default settings will be used.</param>
        /// <returns></returns>
        IEndpointsConfigurationBuilder AddInbound(
            IEndpoint endpoint,
            Func<ErrorPolicyBuilder, IErrorPolicy> errorPolicyFactory = null,
            InboundConnectorSettings settings = null);

        /// <summary>
        /// Adds and inbound endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint (topic).</param>
        /// <param name="errorPolicyFactory">An optional function returning the error policy to be applied in case of
        /// exceptions while consuming the messages from this topic.</param>
        /// <param name="settings">The optional additional settings. If not specified, the default settings will be used.</param>
        /// <typeparam name="TConnector">The type of the <see cref="IInboundConnector"/> to be used.</typeparam>
        /// <returns></returns>
        IEndpointsConfigurationBuilder AddInbound<TConnector>(
            IEndpoint endpoint,
            Func<ErrorPolicyBuilder, IErrorPolicy> errorPolicyFactory = null,
            InboundConnectorSettings settings = null)
            where TConnector : IInboundConnector;

        /// <summary>
        /// Adds and inbound endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint (topic).</param>
        /// <param name="inboundConnectorType">The type of the <see cref="IInboundConnector"/> to be used.
        /// If not specified, the default one will be used.</param>
        /// <param name="errorPolicyFactory">An optional function returning the error policy to be applied in case of
        /// exceptions while consuming the messages from this topic.</param>
        /// <param name="settings">The optional additional settings. If not specified, the default settings will be used.</param>
        /// <returns></returns>
        IEndpointsConfigurationBuilder AddInbound(
            IEndpoint endpoint, 
            Type inboundConnectorType,
            Func<ErrorPolicyBuilder, IErrorPolicy> errorPolicyFactory = null, 
            InboundConnectorSettings settings = null);

        /// <summary>
        /// Enables the legacy behavior where the messages to be routed through an outbound connector are also being
        /// published to the internal bus, to be locally subscribed. This is now disabled by default.
        /// </summary>
        IEndpointsConfigurationBuilder PublishOutboundMessagesToInternalBus();
    }
}