// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.ErrorHandling;

namespace Silverback.Messaging.Configuration
{
    public interface IEndpointsConfigurationBuilder
    {
        EndpointsConfigurationBuilder AddOutbound<TMessage, TConnector>(IEndpoint endpoint)
            where TConnector : IOutboundConnector;

        EndpointsConfigurationBuilder AddOutbound<TMessage>(IEndpoint endpoint, Type outboundConnectorType = null);
        EndpointsConfigurationBuilder AddOutbound(Type messageType, IEndpoint endpoint, Type outboundConnectorType);
        EndpointsConfigurationBuilder AddInbound(IEndpoint endpoint, Func<ErrorPolicyBuilder, IErrorPolicy> errorPolicyFactory = null, InboundConnectorSettings settings = null);

        EndpointsConfigurationBuilder AddInbound<TConnector>(IEndpoint endpoint, Func<ErrorPolicyBuilder, IErrorPolicy> errorPolicyFactory = null, InboundConnectorSettings settings = null)
            where TConnector : IInboundConnector;

        EndpointsConfigurationBuilder AddInbound(IEndpoint endpoint, Type inboundConnectorType, Func<ErrorPolicyBuilder, IErrorPolicy> errorPolicyFactory = null, InboundConnectorSettings settings = null);
    }
}