// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    /// Connects the broker with the inbound and/or outbound endpoints.
    /// </summary>
    public interface IBrokerEndpointsConfigurationBuilder
    {
        IBroker Broker { get; }

        IBrokerEndpointsConfigurationBuilder AddOutbound<TMessage, TConnector>(IEndpoint endpoint) 
            where TMessage : IIntegrationMessage
            where TConnector : IOutboundConnector;

        IBrokerEndpointsConfigurationBuilder AddOutbound<TMessage>(IEndpoint endpoint, Type outboundConnectorType = null) where TMessage : IIntegrationMessage;

        IBrokerEndpointsConfigurationBuilder AddOutbound(IEndpoint endpoint, Type outboundConnectorType = null);

        IBrokerEndpointsConfigurationBuilder AddOutbound(Type messageType, IEndpoint endpoint, Type outboundConnectorType);

        IBrokerEndpointsConfigurationBuilder AddInbound(IEndpoint endpoint, Func<ErrorPolicyBuilder, IErrorPolicy> errorPolicyFactory = null);

        IBrokerEndpointsConfigurationBuilder AddInbound<TConnector>(IEndpoint endpoint, Func<ErrorPolicyBuilder, IErrorPolicy> errorPolicyFactory = null)
            where TConnector : IInboundConnector;

        IBrokerEndpointsConfigurationBuilder AddInbound(IEndpoint endpoint, Type inboundConnectorType, Func<ErrorPolicyBuilder, IErrorPolicy> errorPolicyFactory = null);
    }
}