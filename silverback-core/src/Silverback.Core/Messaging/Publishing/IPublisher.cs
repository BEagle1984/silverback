using System;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing
{
    /// <summary>
    /// Publishes all kind of <see cref="IMessage"/> to the bus.
    /// </summary>
    public interface IPublisher
    {
        #region Events

        /// <summary>
        /// Publishes the specified event to the bus.
        /// </summary>
        /// <param name="message">The event to be published.</param>
        void Publish<TEvent>(TEvent message)
            where TEvent : IEvent;

        /// <summary>
        /// Asynchronously publishes the specified event to the bus.
        /// </summary>
        /// <param name="message">The event to be published.</param>
        /// <returns></returns>
        Task PublishAsync<TEvent>(TEvent message)
            where TEvent : IEvent;

        #endregion

        #region Commands

        /// <summary>
        /// Sends the specified command to the bus.
        /// </summary>
        /// <typeparam name="TCommand">The type of the command.</typeparam>
        /// <param name="message">The command to be sent.</param>
        void Send<TCommand>(TCommand message)
            where TCommand : ICommand;

        /// <summary>
        /// Sends the specified command to the bus.
        /// </summary>
        /// <typeparam name="TCommand">The type of the command.</typeparam>
        /// <param name="message">The command to be sent.</param>
        /// <returns></returns>
        Task SendAsync<TCommand>(TCommand message)
            where TCommand : ICommand;

        #endregion

        #region Requests

        /// <summary>
        /// Sends the specified request to the bus and wait for the response to be received.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request.</typeparam>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="message">The request.</param>
        /// <param name="timeout">The timeout. If not specified, the default timeout of 2 seconds will be used.</param>
        /// <returns></returns>
        TResponse GetResponse<TRequest, TResponse>(TRequest message, TimeSpan? timeout = null)
            where TRequest : IRequest
            where TResponse : IResponse;

        /// <summary>
        /// Sends the specified request to the bus and wait for the response to be received.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request.</typeparam>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="message">The request.</param>
        /// <param name="timeout">The timeout. If not specified, the default timeout of 2 seconds will be used.</param>
        /// <returns></returns>
        Task<TResponse> GetResponseAsync<TRequest, TResponse>(TRequest message, TimeSpan? timeout = null)
            where TRequest : IRequest
            where TResponse : IResponse;

        /// <summary>
        /// Sends the specified request to the bus and wait for the response to be received on another bus.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request.</typeparam>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="replyBus">The reply bus.</param>
        /// <param name="message">The request.</param>
        /// <param name="timeout">The timeout. If not specified, the default timeout of 2 seconds will be used.</param>
        /// <returns></returns>
        TResponse GetResponse<TRequest, TResponse>(IBus replyBus, TRequest message, TimeSpan? timeout = null)
            where TRequest : IRequest
            where TResponse : IResponse;

        /// <summary>
        /// Sends the specified request to the bus and wait for the response to be received on another bus.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request.</typeparam>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="replyBus">The reply bus.</param>
        /// <param name="message">The request.</param>
        /// <param name="timeout">The timeout. If not specified, the default timeout of 2 seconds will be used.</param>
        /// <returns></returns>
        Task<TResponse> GetResponseAsync<TRequest, TResponse>(IBus replyBus, TRequest message, TimeSpan? timeout = null)
            where TRequest : IRequest
            where TResponse : IResponse;

        /// <summary>
        /// Sends the specified response to the bus.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="message">The response.</param>
        void Reply<TResponse>(TResponse message)
            where TResponse : IResponse;

        /// <summary>
        /// Sends the specified response to the bus.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="message">The response.</param>
        Task ReplyAsync<TResponse>(TResponse message)
            where TResponse : IResponse;

        #endregion
    }
}