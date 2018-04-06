using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing
{
    /// <summary>
    /// Publishes the <see cref="IResponse"/> to the bus.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <seealso cref="Silverback.Messaging.Publishing.IResponsePublisher{TResponse}" />
    public class ResponsePublisher<TResponse> : IResponsePublisher<TResponse>
        where TResponse : IResponse
    {
        private readonly IPublisher _publisher;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponsePublisher{TResponse}"/> class.
        /// </summary>
        /// <param name="bus">The bus.</param>
        public ResponsePublisher(IBus bus)
        {
            _publisher = new Publisher(bus);
        }

        /// <summary>
        /// Sends the specified response to the bus.
        /// </summary>
        /// <param name="message">The response.</param>
        public void Reply(TResponse message)
            => _publisher.Reply(message);

        /// <summary>
        /// Sends the specified response to the bus.
        /// </summary>
        /// <param name="message">The response.</param>
        /// <returns></returns>
        public Task ReplyAsync(TResponse message)
            => _publisher.ReplyAsync(message);
    }
}