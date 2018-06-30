using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Subscribers
{
    /// <summary>
    /// Subscribes to the messages published in a bus and executes the specified action for each message.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <seealso cref="AsyncSubscriber{TMessage}" />
    public class GenericAsyncSubscriber<TMessage> : AsyncSubscriber<TMessage>
        where TMessage : IMessage
    {
        private readonly Func<TMessage, Task> _handler;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericAsyncSubscriber{TMessage}" /> class.
        /// </summary>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="handler">The action to be executed for each message.</param>
        /// <param name="filter">An optional filter to be applied to the messages</param>
        /// <exception cref="ArgumentNullException">handler</exception>
        public GenericAsyncSubscriber(ILoggerFactory loggerFactory, Func<TMessage, Task> handler, Func<TMessage, bool> filter = null)
            : base(loggerFactory)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            Filter = filter;
        }

        /// <summary>
        /// Handles the <see cref="T:Silverback.Messaging.Messages.IMessage" /> asynchronously.
        /// </summary>
        /// <param name="message">The message to be handled.</param>
        /// <returns></returns>
        public override Task HandleAsync(TMessage message)
            => _handler(message);
    }
}