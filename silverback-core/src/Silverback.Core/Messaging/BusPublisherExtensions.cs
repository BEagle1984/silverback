using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace Silverback.Messaging
{
    /// <summary>
    /// Provides some extension methods for <see cref="IBus"/> to help building the publishers.
    /// </summary>
    public static class BusPublisherExtensions
    {
        /// <summary>
        /// Gets an instance of <see cref="IPublisher"/> to publish <see cref="IMessage"/> instances to the bus.
        /// </summary>
        /// <param name="bus">The bus.</param>
        /// <returns></returns>
        public static IPublisher GetPublisher(this IBus bus)
        {
            return new Publisher(bus);
        }

        /// <summary>
        /// Gets an instance of <see cref="IEventPublisher{TEvent}"/> to publish <see cref="IEvent"/> instances to the bus.
        /// </summary>
        /// <param name="bus">The bus.</param>
        /// <returns></returns>
        public static IEventPublisher<TEvent> GetEventPublisher<TEvent>(this IBus bus)
            where TEvent : IEvent
        {
            return new EventPublisher<TEvent>(bus);
        }

        /// <summary>
        /// Gets an instance of <see cref="ICommandPublisher{TEvent}"/> to publish <see cref="ICommand"/> instances to the bus.
        /// </summary>
        /// <param name="bus">The bus.</param>
        /// <returns></returns>
        public static ICommandPublisher<TCommand> GetCommandPublisher<TCommand>(this IBus bus)
            where TCommand : ICommand
        {
            return new CommandPublisher<TCommand>(bus);
        }

        /// <summary>
        /// Gets an instance of <see cref="IRequestPublisher{TRequest, TResponse}"/> to publish <see cref="IRequest"/> instances to the bus.
        /// </summary>
        /// <param name="bus">The bus.</param>
        /// <returns></returns>
        public static IRequestPublisher<TRequest, TResponse> GetRequestPublisher<TRequest, TResponse>(this IBus bus)
            where TRequest : IRequest
            where TResponse : IResponse
        {
            return new RequestPublisher<TRequest, TResponse>(bus);
        }

        /// <summary>
        /// Gets an instance of <see cref="IResponsePublisher{TResponse}"/> to publish <see cref="IResponse"/> instances to the bus.
        /// </summary>
        /// <param name="bus">The bus.</param>
        /// <returns></returns>
        public static IResponsePublisher<TResponse> GetResponsePublisher<TResponse>(this IBus bus)
            where TResponse : IResponse
        {
            return new ResponsePublisher<TResponse>(bus);
        }
    }
}