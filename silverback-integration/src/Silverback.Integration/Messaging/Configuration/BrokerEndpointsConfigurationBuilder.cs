using System;
using System.Collections.Generic;
using System.Text;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Messaging.Configuration
{
    public class BrokerEndpointsConfigurationBuilder : IBrokerEndpointsConfigurationBuilder
    {
        private readonly IOutboundRoutingConfiguration _outboundRoutingConfiguration;
        private readonly IEnumerable<IInboundConnector> _inboundConnectors;
        private readonly ErrorPolicyBuilder _errorPolicyBuilder;
        private readonly IBroker _broker;

        public BrokerEndpointsConfigurationBuilder(IOutboundRoutingConfiguration outboundRoutingConfiguration, IEnumerable<IInboundConnector> inboundConnectors, ErrorPolicyBuilder errorPolicyBuilder, IBroker broker)
        {
            _outboundRoutingConfiguration = outboundRoutingConfiguration;
            _inboundConnectors = inboundConnectors;
            _errorPolicyBuilder = errorPolicyBuilder;
            _broker = broker;
        }

        public IBrokerEndpointsConfigurationBuilder AddOutbound<TMessage>(IEndpoint endpoint) where TMessage : IIntegrationMessage
        {
            AddOutbound<TMessage>(endpoint, null);
            return this;
        }

        public IBrokerEndpointsConfigurationBuilder AddOutbound<TMessage, TConnector>(IEndpoint endpoint) 
            where TMessage : IIntegrationMessage
            where TConnector : IOutboundConnector
        {
            AddOutbound<TMessage>(endpoint, typeof(TConnector));
            return this;
        }

        public IBrokerEndpointsConfigurationBuilder AddOutbound<TMessage>(IEndpoint endpoint, Type outboundConnectorType) where TMessage : IIntegrationMessage
        {
            _outboundRoutingConfiguration.Add<TMessage>(endpoint);
            return this;
        }
        
        public IBrokerEndpointsConfigurationBuilder AddInbound(IEndpoint endpoint, Func<ErrorPolicyBuilder, IErrorPolicy> errorPolicyFactory = null)
        {
            AddInbound(endpoint, null, errorPolicyFactory);
            return this;
        }

        public IBrokerEndpointsConfigurationBuilder AddInbound<TConnector>(IEndpoint endpoint, Func<ErrorPolicyBuilder, IErrorPolicy> errorPolicyFactory = null)
            where TConnector : IInboundConnector
        {
            AddInbound(endpoint, typeof(TConnector), errorPolicyFactory);
            return this;
        }

        public IBrokerEndpointsConfigurationBuilder AddInbound(IEndpoint endpoint, Type inboundConnectorType, Func<ErrorPolicyBuilder, IErrorPolicy> errorPolicyFactory = null)
        {
            _inboundConnectors.GetConnectorInstance(inboundConnectorType).Bind(endpoint, errorPolicyFactory?.Invoke(_errorPolicyBuilder));
            return this;
        }

        public void Connect() => _broker.Connect();
    }
}
