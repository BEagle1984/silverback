using System;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Adapters
{
    /// <summary>
    /// Translates the internal <see cref="IMessage"/> into an <see cref="IIntegrationMessage"/> that can be sent over 
    /// the message broker.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <typeparam name="TIntegrationMessage">The type of the integration message.</typeparam>
    /// <seealso cref="Silverback.Messaging.Adapters.MessageTranslator{TMessage, TIntegrationMessage}" />
    public class GenericMessageTranslator<TMessage, TIntegrationMessage> : MessageTranslator<TMessage, TIntegrationMessage>
        where TMessage : IMessage
        where TIntegrationMessage : IIntegrationMessage 
    {
        private readonly Func<TMessage, TIntegrationMessage> _mapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericMessageTranslator{TMessage, TIntegrationMessage}"/> class.
        /// </summary>
        /// <param name="mapper">The mapper.</param>
        /// <param name="bus">The bus.</param>
        /// <param name="filter">An optional filter to be applied to the messages</param>
        public GenericMessageTranslator(Func<TMessage, TIntegrationMessage> mapper, IBus bus, Func<TMessage, bool> filter = null)
            : base(bus, filter)
        {
            _mapper = mapper;
        }

        /// <summary>
        /// Maps the specified <see cref="T:Silverback.Messaging.Messages.IMessage" /> to an <see cref="T:Silverback.Messaging.Messages.IIntegrationMessage" />.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns></returns>
        protected override TIntegrationMessage Map(TMessage source)
            => _mapper(source);
    }
}