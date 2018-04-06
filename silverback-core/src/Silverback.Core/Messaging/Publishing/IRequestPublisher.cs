using System;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing
{
    /// <summary>
    /// Publishes the <see cref="IRequest"/> to the bus.
    /// </summary>
    public interface IRequestPublisher<in TRequest, TResponse>
        where TRequest : IRequest
        where TResponse : IResponse
    {
        /// <summary>
        /// Sends the specified request to the bus and wait for the response to be received.
        /// </summary>
        /// <param name="message">The request.</param>
        /// <param name="timeout">The timeout. If not specified, the default timeout of 2 seconds will be used.</param>
        /// <returns></returns>
        TResponse GetResponse(TRequest message, TimeSpan? timeout = null);

        /// <summary>
        /// Sends the specified request to the bus and wait for the response to be received.
        /// </summary>
        /// <param name="message">The request.</param>
        /// <param name="timeout">The timeout. If not specified, the default timeout of 2 seconds will be used.</param>
        /// <returns></returns>
        Task<TResponse> GetResponseAsync(TRequest message, TimeSpan? timeout = null);

        /// <summary>
        /// Sends the specified request to the bus and wait for the response to be received on another bus.
        /// </summary>
        /// <param name="replyBus">The reply bus.</param>
        /// <param name="message">The request.</param>
        /// <param name="timeout">The timeout. If not specified, the default timeout of 2 seconds will be used.</param>
        /// <returns></returns>
        TResponse GetResponse(IBus replyBus, TRequest message, TimeSpan? timeout = null);

        /// <summary>
        /// Sends the specified request to the bus and wait for the response to be received on another bus.
        /// </summary>
        /// <param name="replyBus">The reply bus.</param>
        /// <param name="message">The request.</param>
        /// <param name="timeout">The timeout. If not specified, the default timeout of 2 seconds will be used.</param>
        /// <returns></returns>
        Task<TResponse> GetResponseAsync(IBus replyBus, TRequest message, TimeSpan? timeout = null);
    }
}