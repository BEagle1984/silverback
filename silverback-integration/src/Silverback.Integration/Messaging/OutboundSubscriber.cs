using System;
using System.Collections.Generic;
using System.Text;
using Silverback.Messaging.Adapters;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging
{
    /// <summary>
    /// The standard subscriber used to attach the <see cref="IOutboundAdapter{TMessage}"/>, suitable for most cases.
    /// In more advanced use cases, when a greater degree of flexibility is required, it is advised to create an ad-hoc implementation of <see cref="Subscriber{TMessage}"/>. 
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <seealso cref="Silverback.Messaging.DefaultSubscriber{TMessage}" />
    public class OutboundSubscriber<TMessage> : Subscriber<TMessage>
        where TMessage : IIntegrationMessage
    {
        private readonly ITypeFactory _typeFactory;
        private readonly Type _handlerType;
        private readonly IEndpoint _endpoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="OutboundSubscriber{TMessage}" /> class.
        /// </summary>
        /// <param name="messages">The observable stream of messages.</param>
        /// <param name="typeFactory">The <see cref="ITypeFactory" /> that will be used to get an <see cref="IMessageHandler{TMessage}" /> instance to process each received message.</param>
        /// <param name="handlerType">Type of the <see cref="IMessageHandler{TMessage}" /> to be used to handle the messages.</param>
        /// <param name="endpoint">The endpoint to be passed to the <see cref="IOutboundAdapter"/>.</param>
        public OutboundSubscriber(IObservable<TMessage> messages, ITypeFactory typeFactory, Type handlerType, IEndpoint endpoint)
            : base(messages)
        {
            _typeFactory = typeFactory ?? throw new ArgumentNullException(nameof(typeFactory));
            _handlerType = handlerType ?? throw new ArgumentNullException(nameof(handlerType));
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));

            _endpoint.ValidateConfiguration();

            if (!typeof(IOutboundAdapter).IsAssignableFrom(handlerType))
                throw new ArgumentException("The specified handler type does not implement IOutboundAdapter.");
        }

        /// <summary>
        /// Called when a message is published.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <exception cref="InvalidOperationException"></exception>
        protected override void OnNext(TMessage message)
        {
            var handler = (IOutboundAdapter)_typeFactory.GetInstance(_handlerType);

            if (handler == null)
                throw new InvalidOperationException($"Couldn't instantiate message handler of type {_handlerType}.");

            handler.Relay(message, _endpoint);
        }
    }
}
