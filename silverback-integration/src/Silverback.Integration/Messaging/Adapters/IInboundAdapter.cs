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
        /// <summary>
        /// Initializes the <see cref="IInboundAdapter" />.
        /// </summary>
        /// <param name="bus">The internal <see cref="IBus" /> where the messages have to be relayed.</param>
        /// <param name="endpoint">The endpoint this adapter has to connect to.</param>
        /// <param name="errorPolicy">An optional error handling policy.</param>
        void Init(IBus bus, IEndpoint endpoint, IErrorPolicy errorPolicy = null);
    }
}