// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.ErrorHandling;

namespace Silverback.Messaging.Configuration
{
    public interface IEndpointsConfigurationBuilder
    {
        IEndpointsConfigurationBuilder AddOutbound<TMessage, TConnector>(IEndpoint endpoint)
            where TConnector : IOutboundConnector;

        IEndpointsConfigurationBuilder AddOutbound<TMessage>(IEndpoint endpoint, Type outboundConnectorType = null);
        IEndpointsConfigurationBuilder AddOutbound(Type messageType, IEndpoint endpoint, Type outboundConnectorType);

        IEndpointsConfigurationBuilder AddInbound(IEndpoint endpoint, Func<ErrorPolicyBuilder, IErrorPolicy> errorPolicyFactory = null, InboundConnectorSettings settings = null);
        IEndpointsConfigurationBuilder AddInbound<TConnector>(IEndpoint endpoint, Func<ErrorPolicyBuilder, IErrorPolicy> errorPolicyFactory = null, InboundConnectorSettings settings = null)
            where TConnector : IInboundConnector;
        IEndpointsConfigurationBuilder AddInbound(IEndpoint endpoint, Type inboundConnectorType, Func<ErrorPolicyBuilder, IErrorPolicy> errorPolicyFactory = null, InboundConnectorSettings settings = null);
        
        IEndpointsConfigurationBuilder PublishOutboundMessagesToInternalBus();
    }
}