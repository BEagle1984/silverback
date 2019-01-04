// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Configuration
{
    public class BrokerEndpointsConfigurationBuilder : IBrokerEndpointsConfigurationBuilder
    {
        private readonly IOutboundRoutingConfiguration _outboundRoutingConfiguration;
        private readonly IEnumerable<IInboundConnector> _inboundConnectors;
        private readonly ErrorPolicyBuilder _errorPolicyBuilder;

        public BrokerEndpointsConfigurationBuilder(IOutboundRoutingConfiguration outboundRoutingConfiguration, IEnumerable<IInboundConnector> inboundConnectors, ErrorPolicyBuilder errorPolicyBuilder, IBroker broker)
        {
            _outboundRoutingConfiguration = outboundRoutingConfiguration;
            _inboundConnectors = inboundConnectors;
            _errorPolicyBuilder = errorPolicyBuilder;
            Broker = broker;
        }

        public IBroker Broker { get; }

        public IBrokerEndpointsConfigurationBuilder AddOutbound<TMessage, TConnector>(IEndpoint endpoint) 
            where TMessage : IIntegrationMessage
            where TConnector : IOutboundConnector
        {
            AddOutbound<TMessage>(endpoint, typeof(TConnector));
            return this;
        }

        public IBrokerEndpointsConfigurationBuilder AddOutbound<TMessage>(IEndpoint endpoint, Type outboundConnectorType = null) where TMessage : IIntegrationMessage
        {
            AddOutbound(typeof(TMessage), endpoint, outboundConnectorType);
            return this;
        }
        
        public IBrokerEndpointsConfigurationBuilder AddOutbound(IEndpoint endpoint, Type outboundConnectorType = null)
        {
            AddOutbound(typeof(IIntegrationMessage), endpoint, outboundConnectorType);
            return this;
        }

        public IBrokerEndpointsConfigurationBuilder AddOutbound(Type messageType, IEndpoint endpoint, Type outboundConnectorType)
        {
            _outboundRoutingConfiguration.Add(messageType, endpoint, outboundConnectorType);
            return this;
        }

        public IBrokerEndpointsConfigurationBuilder AddInbound(IEndpoint endpoint, Func<ErrorPolicyBuilder, IErrorPolicy> errorPolicyFactory = null, InboundConnectorSettings settings = null)
        {
            AddInbound(endpoint, null, errorPolicyFactory, settings);
            return this;
        }

        public IBrokerEndpointsConfigurationBuilder AddInbound<TConnector>(IEndpoint endpoint, Func<ErrorPolicyBuilder, IErrorPolicy> errorPolicyFactory = null, InboundConnectorSettings settings = null)
            where TConnector : IInboundConnector
        {
            AddInbound(endpoint, typeof(TConnector), errorPolicyFactory, settings);
            return this;
        }

        public IBrokerEndpointsConfigurationBuilder AddInbound(IEndpoint endpoint, Type inboundConnectorType, Func<ErrorPolicyBuilder, IErrorPolicy> errorPolicyFactory = null, InboundConnectorSettings settings = null)
        {
            _inboundConnectors.GetConnectorInstance(inboundConnectorType).Bind(endpoint, errorPolicyFactory?.Invoke(_errorPolicyBuilder), settings);
            return this;
        }
    }
}
