using Silverback.Messaging.Broker;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Publishing;

namespace Silverback.Messaging.Adapters
{
    /// <summary>
    /// An adapter that subscribes to the message broker and forwards the messages to the internal bus.
    /// </summary>
    public interface IInboundAdapter
    {
        void Init(IBus bus, IEndpoint endpoint, IErrorPolicy errorPolicy = null);
    }
}