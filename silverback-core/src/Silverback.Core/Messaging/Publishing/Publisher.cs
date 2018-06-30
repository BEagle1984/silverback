using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Messaging.Publishing
{
    /// <summary>
    /// Publishes all kind of <see cref="IMessage"/> to the bus.
    /// </summary>
    /// <seealso cref="Silverback.Messaging.Publishing.IPublisher" />
    public class Publisher : IPublisher
    {
        private readonly IBus _bus;

        /// <summary>
        /// Initializes a new instance of the <see cref="Publisher"/> class.
        /// </summary>
        /// <param name="bus">The bus.</param>
        public Publisher(IBus bus)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        #region Events

        /// <summary>
        /// Publishes the specified event to the bus.
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        /// <param name="message">The event to be published.</param>
        public void Publish<TEvent>(TEvent message)
            where TEvent : IEvent
        {
            _bus.Publish(message);
        }

        /// <summary>
        /// Asynchronously publishes the specified event to the bus.
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        /// <param name="message">The event to be published.</param>
        /// <returns></returns>
        public Task PublishAsync<TEvent>(TEvent message)
            where TEvent : IEvent
        {
            return _bus.PublishAsync(message);
        }

        #endregion

        #region Commands

        /// <summary>
        /// Sends the specified command to the bus.
        /// </summary>
        /// <typeparam name="TCommand">The type of the command.</typeparam>
        /// <param name="message">The command to be sent.</param>
        public void Send<TCommand>(TCommand message)
            where TCommand : ICommand
        {
            _bus.Publish(message);
        }

        /// <summary>
        /// Sends the specified command to the bus.
        /// </summary>
        /// <typeparam name="TCommand">The type of the command.</typeparam>
        /// <param name="message">The command to be sent.</param>
        /// <returns></returns>
        public Task SendAsync<TCommand>(TCommand message)
            where TCommand : ICommand
        {
            return _bus.PublishAsync(message);
        }

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
        public TResponse GetResponse<TRequest, TResponse>(TRequest message, TimeSpan? timeout = null)
            where TRequest : IRequest
            where TResponse : IResponse
        {
            return GetResponse<TRequest, TResponse>(_bus, message, timeout);
        }

        /// <summary>
        /// Sends the specified request to the bus and wait for the response to be received.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request.</typeparam>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="message">The request.</param>
        /// <param name="timeout">The timeout. If not specified, the default timeout of 2 seconds will be used.</param>
        /// <returns></returns>
        public Task<TResponse> GetResponseAsync<TRequest, TResponse>(TRequest message, TimeSpan? timeout = null)
            where TRequest : IRequest
            where TResponse : IResponse
        {
            return GetResponseAsync<TRequest, TResponse>(_bus, message, timeout);
        }

        /// <summary>
        /// Sends the specified request to the bus and wait for the response to be received on another bus.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request.</typeparam>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="replyBus">The reply bus.</param>
        /// <param name="message">The request.</param>
        /// <param name="timeout">The timeout. If not specified, the default timeout of 2 seconds will be used.</param>
        /// <returns></returns>
        public TResponse GetResponse<TRequest, TResponse>(IBus replyBus, TRequest message, TimeSpan? timeout = null)
            where TRequest : IRequest
            where TResponse : IResponse
        {
            return GetResponseAsync<TRequest, TResponse>(replyBus, message, timeout).Result;
        }

        /// <summary>
        /// Sends the specified request to the bus and wait for the response to be received on another bus.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request.</typeparam>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="replyBus">The reply bus.</param>
        /// <param name="message">The request.</param>
        /// <param name="timeout">The timeout. If not specified, the default timeout of 2 seconds will be used.</param>
        /// <returns></returns>
        /// <exception cref="TimeoutException"></exception>
        public async Task<TResponse> GetResponseAsync<TRequest, TResponse>(IBus replyBus, TRequest message, TimeSpan? timeout = null)
            where TRequest : IRequest
            where TResponse : IResponse
        {
            timeout = timeout ?? TimeSpan.FromSeconds(2);

            CheckRequestMessage(message);

            TResponse response = default;

            var replySubscriber = replyBus.Subscribe(
                new GenericSubscriber<TResponse>(
                    m => response = m,
                    m => m.RequestId == message.RequestId));

            try
            {
                _bus.Publish(message);

                var stopwatch = new Stopwatch();
                stopwatch.Start();

                while (response == null)
                {
                    if (stopwatch.Elapsed >= timeout)
                        throw new TimeoutException($"The request with id {message.RequestId} was not replied in the allotted time.");

                    await Task.Delay(50); // TODO: Check this!
                }

                stopwatch.Stop();

                return response;
            }
            finally
            {
                replyBus.Unsubscribe(replySubscriber);
            }
        }

        /// <summary>
        /// Checks the request message ensuring that the RequestId is set.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request.</typeparam>
        /// <param name="message">The message.</param>
        /// <exception cref="ArgumentNullException">message</exception>
        private void CheckRequestMessage<TRequest>(TRequest message) where TRequest : IRequest
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            if (message.RequestId == Guid.Empty)
                message.RequestId = Guid.NewGuid();
        }

        /// <summary>
        /// Sends the specified response to the bus.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="message">The response.</param>
        public void Reply<TResponse>(TResponse message)
            where TResponse : IResponse
        {
            _bus.Publish(message);
        }

        /// <summary>
        /// Sends the specified response to the bus.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="message">The response.</param>
        /// <returns></returns>
        public Task ReplyAsync<TResponse>(TResponse message)
            where TResponse : IResponse
        {
            return _bus.PublishAsync(message);
        }

        #endregion
    }
}
