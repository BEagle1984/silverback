using System;
using System.Reactive.Linq;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging
{
    // TODO: Review summaries
    /// <summary>
    /// The standard subscriber, suitable for most cases.
    /// In more advanced use cases, when a greater degree of flexibility is required, it is advised to create an ad-hoc implementation of <see cref="Subscriber{TMessage}"/>. 
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <seealso cref="System.IDisposable" />
    public class DefaultSubscriber<TMessage> : Subscriber<TMessage>
        where TMessage : IMessage
    {
        private readonly ITypeFactory _typeFactory;
        private readonly Type _handlerType;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultSubscriber{TMessage}" /> class.
        /// </summary>
        /// <param name="messages">The observable stream of messages.</param>
        /// <param name="typeFactory">The <see cref="ITypeFactory" /> that will be used to get an <see cref="IMessageHandler{TMessage}" /> instance to process each received message.</param>
        /// <param name="handlerType">Type of the <see cref="IMessageHandler{TMessage}" /> to be used to handle the messages.</param>
        /// <param name="filter">An optional filter to be applied to the published messages.</param>
        public DefaultSubscriber(IObservable<TMessage> messages, ITypeFactory typeFactory, Type handlerType, Func<TMessage, bool> filter = null)
            : base(ApplyFilter(messages, filter))
        {
            _typeFactory = typeFactory ?? throw new ArgumentNullException(nameof(typeFactory));
            _handlerType = handlerType ?? throw new ArgumentNullException(nameof(handlerType));

            if (!typeof(IMessageHandler).IsAssignableFrom(handlerType))
                throw new ArgumentException("The specified handler type does not implement IMessageHandler.");
        }

        /// <summary>
        /// Applies the filter.
        /// </summary>
        /// <param name="messages">The message stream.</param>
        /// <param name="filter">The filter to be applied.</param>
        /// <returns></returns>
        private static IObservable<TMessage> ApplyFilter(IObservable<TMessage> messages, Func<TMessage, bool> filter)
        {
            if (filter != null)
                return messages?.Where(filter);

            return messages;
        }

        /// <summary>
        /// Called when a message is published.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <exception cref="InvalidOperationException"></exception>
        protected override void OnNext(TMessage message)
        {
            var handler = (IMessageHandler)_typeFactory.GetInstance(_handlerType);

            if (handler == null)
                throw new InvalidOperationException($"Couldn't instantiate message handler of type {_handlerType}.");

            handler.Handle(message);
        }
    }
}
