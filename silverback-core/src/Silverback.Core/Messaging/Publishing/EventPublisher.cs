using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing
{
    /// <summary>
    /// Publishes the <see cref="IEvent" /> to the bus.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    /// <seealso cref="Silverback.Messaging.Publishing.IEventPublisher{TEvent}" />
    public class EventPublisher<TEvent> : IEventPublisher<TEvent>
        where TEvent : IEvent
    {
        private readonly IPublisher _publisher;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventPublisher{TEvent}"/> class.
        /// </summary>
        /// <param name="bus">The bus.</param>
        public EventPublisher(IBus bus)
        {
            _publisher = new Publisher(bus);
        }

        /// <summary>
        /// Publishes the specified event to the bus.
        /// </summary>
        /// <param name="message">The event to be published.</param>
        public void Publish(TEvent message)
            => _publisher.Publish(message);

        /// <summary>
        /// Asynchronously publishes the specified event to the bus.
        /// </summary>
        /// <param name="message">The event to be published.</param>
        /// <returns></returns>
        public Task PublishAsync(TEvent message)
            => _publisher.PublishAsync(message);
    }
}