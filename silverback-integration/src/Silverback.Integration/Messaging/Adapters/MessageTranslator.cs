using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Adapters
{
    /// <summary>
    /// Translates the internal <see cref="IMessage"/> into an <see cref="IIntegrationMessage"/> that can be sent over 
    /// the message broker.
    /// </summary>
    public abstract class MessageTranslator<TMessage, TIntegrationMessage> : MessageHandler<TMessage> 
        where TMessage : IMessage
        where TIntegrationMessage : IIntegrationMessage
    {
        private readonly IBus _bus;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageTranslator{TMessage, TIntegrationMessage}"/> class.
        /// </summary>
        /// <param name="bus">The bus.</param>
        protected MessageTranslator(IBus bus)
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