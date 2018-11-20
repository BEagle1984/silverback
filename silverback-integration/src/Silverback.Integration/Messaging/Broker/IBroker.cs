using Microsoft.Extensions.Logging;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    /// The basic interface to interact with the message broker.
    /// </summary>
    public interface IBroker
    {
        IProducer GetProducer(IEndpoint endpoint);

        IConsumer GetConsumer(IEndpoint endpoint);

        bool IsConnected { get; }

        void Connect();
        
        void Disconnect();
    }
}
