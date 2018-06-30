using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Subscribers
{
    /// <summary>
    /// Configures the <see cref="MultiSubscriber"/>.
    /// </summary>
    public class MultiSubscriberConfig
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly List<ISubscriber> _handlers = new List<ISubscriber>();

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiSubscriberConfig"/> class.
        /// </summary>
        /// <param name="loggerFactory">The logger factory.</param>
        public MultiSubscriberConfig(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        /// <summary>
        /// Adds an handler for all messages.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="filter">The filter.</param>
        /// <returns></returns>
        public MultiSubscriberConfig AddHandler(Action<IMessage> handler, Func<IMessage, bool> filter = null)
        {
            _handlers.Add(new GenericSubscriber<IMessage>(_loggerFactory, handler, filter));

            return this;
        }

        /// <summary>
        /// Adds an handler for the messages of type TMessage.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="handler">The handler.</param>
        /// <param name="filter">The filter.</param>
        /// <returns></returns>
        public MultiSubscriberConfig AddHandler<TMessage>(Action<TMessage> handler, Func<TMessage, bool> filter = null)
            where TMessage : IMessage
        {
            _handlers.Add(new GenericSubscriber<TMessage>(handler, filter));

            return this;
        }

        /// <summary>
        /// Adds an handler for all messages.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="filter">The filter.</param>
        /// <returns></returns>
        public MultiSubscriberConfig AddAsyncHandler(Func<IMessage, Task> handler, Func<IMessage, bool> filter = null)
        {
            _handlers.Add(new GenericAsyncSubscriber<IMessage>(handler, filter));

            return this;
        }
        /// <summary>
        /// Adds an handler for the messages of type TMessage.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="handler">The handler.</param>
        /// <param name="filter">The filter.</param>
        /// <returns></returns>
        public MultiSubscriberConfig AddAsyncHandler<TMessage>(Func<TMessage, Task> handler, Func<TMessage, bool> filter = null)
            where TMessage : IMessage
        {
            _handlers.Add(new GenericAsyncSubscriber<TMessage>(handler, filter));

            return this;
        }

        /// <summary>
        /// Gets the configured message handlers.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ISubscriber> GetHandlers() 
            => _handlers;
    }
}