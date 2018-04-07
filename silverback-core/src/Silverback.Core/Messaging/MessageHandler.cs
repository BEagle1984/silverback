using System;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging
{
    /// <summary>
    /// Handles the <see cref="IMessage"/> of type TMessage.
    /// </summary>
    /// <typeparam name="TMessage">The type of <see cref="IMessage"/> to be handled.</typeparam>
    /// <seealso cref="Silverback.Messaging.IMessageHandler" />
    public abstract class MessageHandler<TMessage> : IMessageHandler
        where TMessage : IMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageHandler{TMessage}"/> class.
        /// </summary>
        /// <param name="filter">An optional filter to be applied to the messages</param>
        protected MessageHandler(Func<TMessage, bool> filter = null)
        {
            Filter = filter;
        }

        /// <summary>
        /// Gets or sets an optional filter to be applied to the messages.
        /// </summary>
        public Func<TMessage, bool> Filter { get; set; }

        /// <summary>
        /// Handles the <see cref="T:Silverback.Messaging.Messages.IMessage" />.
        /// </summary>
        /// <param name="message">The message to be handled.</param>
        public abstract void Handle(TMessage message);

        /// <summary>
        /// Handles the <see cref="T:Silverback.Messaging.Messages.IMessage" />.
        /// </summary>
        /// <param name="message">The message to be handled.</param>
        void IMessageHandler.Handle(IMessage message)
        {
            // TODO: Tracing

            if (!(message is TMessage))
                return;

            var typedMessage = (TMessage)message;

            if (Filter != null && !Filter.Invoke(typedMessage))
                return;

            Handle((TMessage)message);
        }
    }
}
