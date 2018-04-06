using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing
{
    /// <summary>
    /// Publishes the <see cref="IEvent"/> to the bus.
    /// </summary>
    public interface IEventPublisher<in TEvent>
        where TEvent : IEvent
    {
        /// <summary>
        /// Publishes the specified event to the bus.
        /// </summary>
        /// <param name="message">The event to be published.</param>
        void Publish(TEvent message);

        /// <summary>
        /// Asynchronously publishes the specified event to the bus.
        /// </summary>
        /// <param name="message">The event to be published.</param>
        /// <returns></returns>
        Task PublishAsync(TEvent message);
    }
}