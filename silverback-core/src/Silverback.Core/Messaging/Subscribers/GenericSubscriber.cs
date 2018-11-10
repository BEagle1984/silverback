using System;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Subscribers
{
    /// <summary>
    /// Subscribes to the messages published in a bus and executes the specified action for each message.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <seealso cref="Subscriber{TMessage}" />
    public class GenericSubscriber<TMessage> : Subscriber<TMessage>
            where TMessage : IMessage
    {
        private readonly Action<TMessage> _handler;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericSubscriber{TMessage}" /> class.
        /// </summary>
        /// <param name="handler">The action to be executed for each message.</param>
        /// <param name="filter">An optional filter to be applied to the messages</param>
        public GenericSubscriber(Action<TMessage> handler, Func<TMessage, bool> filter = null)
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
