using System;
using System.Collections.Generic;
using System.Text;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Configuration
{
    public class BrokerEndpointsConfigurationBuilder : IBrokerEndpointsConfigurationBuilder
    {
        private readonly IOutboundRoutingConfiguration _outboundRoutingConfiguration;
        private readonly IInboundConnector _inboundConnector;
        private readonly ErrorPolicyBuilder _errorPolicyBuilder;
        private readonly IBroker _broker;

        public BrokerEndpointsConfigurationBuilder(IOutboundRoutingConfiguration outboundRoutingConfiguration, IInboundConnector inboundConnector, ErrorPolicyBuilder errorPolicyBuilder, IBroker broker)
        {
            _outboundRoutingConfiguration = outboundRoutingConfiguration;
            _inboundConnector = inboundConnector;
            _errorPolicyBuilder = errorPolicyBuilder;
            _broker = broker;
        }

        public IBrokerEndpointsConfigurationBuilder AddOutbound<TMessage>(IEndpoint endpoint) where TMessage : IIntegrationMessage
        {
            _outboundRoutingConfiguration.Add<TMessage>(endpoint);
            return this;
        }

        public IBrokerEndpointsConfigurationBuilder AddInbound(IEndpoint endpoint, Func<ErrorPolicyBuilder, IErrorPolicy> errorPolicyFactory = null)
        {
            _inboundConnector.Bind(endpoint, errorPolicyFactory?.Invoke(_errorPolicyBuilder));
            return this;
        }

        public void Connect() => _broker.Connect();
    }
}
