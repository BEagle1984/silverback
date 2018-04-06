using System;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing
{
    /// <summary>
    /// Publishes the <see cref="IRequest"/> to the bus.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <seealso cref="Silverback.Messaging.Publishing.IRequestPublisher{TRequest, TResponse}" />
    public class RequestPublisher<TRequest, TResponse> : IRequestPublisher<TRequest, TResponse>
        where TRequest : IRequest
        where TResponse : IResponse
    {
        private readonly IPublisher _publisher;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestPublisher{TRequest, TResponse}"/> class.
        /// </summary>
        /// <param name="bus">The bus.</param>
        public RequestPublisher(IBus bus)
        {
            _publisher = new Publisher(bus);
        }

        /// <summary>
        /// Sends the specified request to the bus and wait for the response to be received.
        /// </summary>
        /// <param name="message">The request.</param>
        /// <param name="timeout">The timeout. If not specified, the default timeout of 2 seconds will be used.</param>
        /// <returns></returns>
        public TResponse GetResponse(TRequest message, TimeSpan? timeout = null) 
            => _publisher.GetResponse<TRequest, TResponse>(message, timeout);

        /// <summary>
        /// Sends the specified request to the bus and wait for the response to be received.
        /// </summary>
        /// <param name="message">The request.</param>
        /// <param name="timeout">The timeout. If not specified, the default timeout of 2 seconds will be used.</param>
        /// <returns></returns>
        public Task<TResponse> GetResponseAsync(TRequest message, TimeSpan? timeout = null)
            => _publisher.GetResponseAsync<TRequest, TResponse>( message, timeout);

        /// <summary>
        /// Sends the specified request to the bus and wait for the response to be received on another bus.
        /// </summary>
        /// <param name="replyBus">The reply bus.</param>
        /// <param name="message">The request.</param>
        /// <param name="timeout">The timeout. If not specified, the default timeout of 2 seconds will be used.</param>
        /// <returns></returns>
        public TResponse GetResponse(IBus replyBus, TRequest message, TimeSpan? timeout = null)
            => _publisher.GetResponse<TRequest, TResponse>(replyBus, message, timeout);

        /// <summary>
        /// Sends the specified request to the bus and wait for the response to be received on another bus.
        /// </summary>
        /// <param name="replyBus">The reply bus.</param>
        /// <param name="message">The request.</param>
        /// <param name="timeout">The timeout. If not specified, the default timeout of 2 seconds will be used.</param>
        /// <returns></returns>
        public Task<TResponse> GetResponseAsync(IBus replyBus, TRequest message, TimeSpan? timeout = null)
            => _publisher.GetResponseAsync<TRequest, TResponse>(replyBus, message, timeout);
    }
}