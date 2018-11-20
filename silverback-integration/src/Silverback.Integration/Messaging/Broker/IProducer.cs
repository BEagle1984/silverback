using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    public interface IProducer
    {
        void Produce(IMessage message);

        Task ProduceAsync(IMessage message);
    }
}