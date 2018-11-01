using System;
using Silverback.Extensions;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Subscribers
{
    /// <summary>
    /// Subscribes to the messages published in a bus.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <seealso cref="ISubscriber" />
    public abstract class AsyncSubscriber<TMessage> : SubscriberBase<TMessage>
        where TMessage : IMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncSubscriber{TMessage}" /> class.
        /// </summary>
        /// <param name="filter">An optional filter to be applied to the messages</param>
        protected AsyncSubscriber(Func<TMessage, bool> filter = null)
            : base(filter)
        {
        }

        /// <summary>
        /// Handles the <see cref="T:Silverback.Messaging.Messages.IMessage" />.
        /// </summary>
        /// <param name="message">The message to be handled.</param>
        public sealed override void Handle(TMessage message)
        {
            AsyncHelper.RunSynchronously(() => HandleAsync(message));
        }
    }
}