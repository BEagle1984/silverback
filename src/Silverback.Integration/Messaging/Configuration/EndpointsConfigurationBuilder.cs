// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.ErrorHandling;

namespace Silverback.Messaging.Configuration
{
    public class EndpointsConfigurationBuilder : IEndpointsConfigurationBuilder
    {
        private readonly IOutboundRoutingConfiguration _outboundRoutingConfiguration;
        private readonly IEnumerable<IInboundConnector> _inboundConnectors;
        private readonly ErrorPolicyBuilder _errorPolicyBuilder;

        public EndpointsConfigurationBuilder(IOutboundRoutingConfiguration outboundRoutingConfiguration, IEnumerable<IInboundConnector> inboundConnectors, ErrorPolicyBuilder errorPolicyBuilder)
        {
            _outboundRoutingConfiguration = outboundRoutingConfiguration;
            _inboundConnectors = inboundConnectors;
            _errorPolicyBuilder = errorPolicyBuilder;
        }

        public IEndpointsConfigurationBuilder AddOutbound<TMessage, TConnector>(IProducerEndpoint endpoint)
            where TConnector : IOutboundConnector
        {
            AddOutbound<TMessage>(endpoint, typeof(TConnector));
            return this;
        }

        public IEndpointsConfigurationBuilder AddOutbound<TMessage>(IProducerEndpoint endpoint, Type outboundConnectorType = null)
        {
            AddOutbound(typeof(TMessage), endpoint, outboundConnectorType);
            return this;
        }

        public IEndpointsConfigurationBuilder AddOutbound(Type messageType, IProducerEndpoint endpoint, Type outboundConnectorType)
        {
            _outboundRoutingConfiguration.Add(messageType, endpoint, outboundConnectorType);
            return this;
        }

        public IEndpointsConfigurationBuilder AddInbound(IConsumerEndpoint endpoint, Func<ErrorPolicyBuilder, IErrorPolicy> errorPolicyFactory = null, InboundConnectorSettings settings = null)
        {
            AddInbound(endpoint, null, errorPolicyFactory, settings);
            return this;
        }

        public IEndpointsConfigurationBuilder AddInbound<TConnector>(IConsumerEndpoint endpoint, Func<ErrorPolicyBuilder, IErrorPolicy> errorPolicyFactory = null, InboundConnectorSettings settings = null)
            where TConnector : IInboundConnector
        {
            AddInbound(endpoint, typeof(TConnector), errorPolicyFactory, settings);
            return this;
        }

        public IEndpointsConfigurationBuilder AddInbound(IConsumerEndpoint endpoint, Type inboundConnectorType, Func<ErrorPolicyBuilder, IErrorPolicy> errorPolicyFactory = null, InboundConnectorSettings settings = null)
        {
            _inboundConnectors.GetConnectorInstance(inboundConnectorType).Bind(endpoint, errorPolicyFactory?.Invoke(_errorPolicyBuilder), settings);
            return this;
        }

        public IEndpointsConfigurationBuilder PublishOutboundMessagesToInternalBus()
        {
            _outboundRoutingConfiguration.PublishOutboundMessagesToInternalBus = true;
            return this;
        }
    }
}