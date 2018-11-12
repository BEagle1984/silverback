using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing
{
    public interface IEventPublisher<in TEvent>
        where TEvent : IEvent
    {
        void Publish(TEvent eventMessage);

        Task PublishAsync(TEvent eventMessage);
    }
}