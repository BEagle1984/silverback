using System;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging
{
    /// <summary>
    /// Handles the <see cref="IMessage"/> of type <see cref="TMessage"/> executing the provided action.
    /// </summary>
    /// <typeparam name="TMessage">The type of <see cref="IMessage"/> to be handled.</typeparam>
    /// <seealso cref="MessageHandler{TMessage}" />
    public class GenericMessageHandler<TMessage> : MessageHandler<TMessage>
        where TMessage : IMessage
    {
        private readonly Action<TMessage> _handler;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericMessageHandler{TMessage}"/> class.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="filter">An optional filter to be applied to the messages</param>
        public GenericMessageHandler(Action<TMessage> handler, Func<TMessage, bool> filter = null)
            : base(filter)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        /// <summary>
        /// Handles the <see cref="T:Silverback.Messaging.Messages.IMessage" />.
        /// </summary>
        /// <param name="message">The message to be handled.</param>
        public override void Handle(TMessage message)
            => _handler(message);
    }
}
