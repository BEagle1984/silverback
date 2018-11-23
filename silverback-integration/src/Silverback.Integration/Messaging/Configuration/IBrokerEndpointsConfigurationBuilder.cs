using System;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    /// Connects the broker with the inbound and/or outbound endpoints.
    /// </summary>
    public interface IBrokerEndpointsConfigurationBuilder
    {
        IBrokerEndpointsConfigurationBuilder AddOutbound<TMessage>(IEndpoint endpoint) where TMessage : IIntegrationMessage;

        IBrokerEndpointsConfigurationBuilder AddInbound(IEndpoint endpoint, Func<ErrorPolicyBuilder, IErrorPolicy> errorPolicyFactory = null);

        void Connect();
    }
}