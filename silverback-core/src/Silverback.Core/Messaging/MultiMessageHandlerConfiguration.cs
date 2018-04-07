using System;
using System.Collections.Generic;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging
{
    /// <summary>
    /// Configures the <see cref="MultiMessageHandler"/>.
    /// </summary>
    public class MultiMessageHandlerConfiguration
    {
        private readonly List<IMessageHandler> _handlers = new List<IMessageHandler>();

        /// <summary>
        /// Adds an handler for all messages.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="filter">The filter.</param>
        /// <returns></returns>
        public MultiMessageHandlerConfiguration AddHandler(Action<IMessage> handler, Func<IMessage, bool> filter = null)
        {
            _handlers.Add(new GenericMessageHandler<IMessage>(handler, filter));

            return this;
        }

        /// <summary>
        /// Adds an handler for the messages of type TMessage.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="handler">The handler.</param>
        /// <param name="filter">The filter.</param>
        /// <returns></returns>
        public MultiMessageHandlerConfiguration AddHandler<TMessage>(Action<TMessage> handler, Func<TMessage, bool> filter = null)
            where TMessage : IMessage
        {
            _handlers.Add(new GenericMessageHandler<TMessage>(handler, filter));

            return this;
        }

        /// <summary>
        /// Gets the configured message handlers.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IMessageHandler> GetHandlers() 
            => _handlers;
    }
}