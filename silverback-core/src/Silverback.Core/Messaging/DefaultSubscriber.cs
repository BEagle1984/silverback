using System;
using System.Reactive.Linq;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging
{
    // TODO: Review summaries
    /// <summary>
    /// The standard subscriber, suitable for most cases.
    /// In more advanced use cases, when a greater degree of flexibility is required, it is advised to create an ad-hoc implementation of <see cref="Subscriber"/>. 
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public class DefaultSubscriber : Subscriber
    {
        private readonly ITypeFactory _typeFactory;
        private readonly Type _handlerType;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultSubscriber" /> class.
        /// </summary>
        /// <param name="messages">The observable stream of messages.</param>
        /// <param name="typeFactory">The <see cref="ITypeFactory" /> that will be used to get an <see cref="IMessageHandler" /> instance to process each received message.</param>
        /// <param name="handlerType">Type of the <see cref="IMessageHandler" /> to be used to handle the messages.</param>
        public DefaultSubscriber(IObservable<IMessage> messages, ITypeFactory typeFactory, Type handlerType)
            : base(messages)
        {
            _typeFactory = typeFactory ?? throw new ArgumentNullException(nameof(typeFactory));
            _handlerType = handlerType ?? throw new ArgumentNullException(nameof(handlerType));

            if (!typeof(IMessageHandler).IsAssignableFrom(handlerType))
                throw new ArgumentException("The specified handler type does not implement IMessageHandler.");
        }

        /// <summary>
        /// Called when a message is published.
        /// </summary>
        /// <param name="message">The message.</param>
        protected override void OnNext(IMessage message)
        {
            var handler = (IMessageHandler)_typeFactory.GetInstance(_handlerType);

            if (handler == null)
                throw new InvalidOperationException($"Couldn't instantiate message handler of type {_handlerType}.");

            handler.Handle(message);
        }
    }
}
