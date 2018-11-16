using System;
using Silverback.Messaging.ErrorHandling;

namespace Silverback.Messaging.Connectors
{
    /// <summary>
    /// Subscribes to a message broker and forwards the incoming integration messages to the internal bus.
    /// </summary>
    public interface IInboundConnector
    {
        IInboundConnector Bind(IEndpoint endpoint, Func<ErrorPolicyBuilder, IErrorPolicy> errorPolicyFactory = null);
    }
}