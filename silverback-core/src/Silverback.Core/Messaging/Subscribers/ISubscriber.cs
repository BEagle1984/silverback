using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Subscribers
{
    /// <summary>
    /// Subscribes to the messages published in a bus.
    /// </summary>
    public interface ISubscriber
    {
        void Init(IBus bus);

        void OnNext(IMessage message);

        Task OnNextAsync(IMessage message);
    }
}