using Silverback.Messaging.ErrorHandling;

namespace Silverback.Messaging.Integration
{
    /// <summary>
    /// Subscribes to a message broker and forwards the incoming integration messages to the internal bus.
    /// </summary>
    public interface IInboundConnector
    {
        void Init(IBus bus, IEndpoint endpoint, IErrorPolicy errorPolicy = null);
    }
}