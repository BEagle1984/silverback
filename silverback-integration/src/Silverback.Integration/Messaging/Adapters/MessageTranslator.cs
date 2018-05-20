using System;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Messaging.Adapters
{
    /// <summary>
    /// Translates the internal <see cref="IMessage"/> into an <see cref="IIntegrationMessage"/> that can be sent over 
    /// the message broker.
    /// </summary>
    public abstract class MessageTranslator<TMessage, TIntegrationMessage> : Subscriber<TMessage> 
        where TMessage : IMessage
        where TIntegrationMessage : IIntegrationMessage
    {
        private readonly IBus _bus;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageTranslator{TMessage, TIntegrationMessage}"/> class.
        /// </summary>
        /// <param name="bus">The bus.</param>
        /// <param name="filter">An optional filter to be applied to the messages</param>
        protected MessageTranslator(IBus bus, Func<TMessage, bool> filter = null)
            : base(filter)
        {
            _bus = bus;
        }

        /// <summary>
        /// Handles the <see cref="T:Silverback.Messaging.Messages.IMessage" />.
        /// </summary>
        /// <param name="message">The message to be handled.</param>
        public override void Handle(TMessage message)
        {
            _bus.Publish(Map(message));
        }

        /// <summary>
        /// Maps the specified <see cref="IMessage"/> to an <see cref="IIntegrationMessage"/>.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns></returns>
        protected abstract TIntegrationMessage Map(TMessage source);
    }
}