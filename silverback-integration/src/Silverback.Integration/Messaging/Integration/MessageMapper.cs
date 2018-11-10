using System;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Messaging.Integration
{
    /// <summary>
    /// Translates the internal <see cref="IMessage"/> into an <see cref="IIntegrationMessage"/> that can be sent over 
    /// the message broker.
    /// </summary>
    public abstract class MessageMapper<TMessage, TIntegrationMessage> : Subscriber<TMessage> 
        where TMessage : IMessage
        where TIntegrationMessage : IIntegrationMessage
    {
        private IBus _bus;
        private ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageMapper{TMessage,TIntegrationMessage}" /> class.
        /// </summary>
        /// <param name="filter">An optional filter to be applied to the messages</param>
        protected MessageMapper(Func<TMessage, bool> filter = null)
            : base(filter)
        {
        }

        /// <summary>
        /// Initializes the mapper, binding to the specified bus.
        /// </summary>
        /// <param name="bus">The bus.</param>
        public override void Init(IBus bus)
        {
            _bus = bus;
            _logger = bus.GetLoggerFactory().CreateLogger<MessageMapper<TMessage, TIntegrationMessage>>();
            base.Init(bus);
        }

        /// <summary>
        /// Handles the <see cref="T:Silverback.Messaging.Messages.IMessage" />.
        /// </summary>
        /// <param name="message">The message to be handled.</param>
        public override void Handle(TMessage message)
        {
            if (_bus == null) throw new InvalidOperationException("Not initialized.");

            _logger.LogTrace($"Mapping message of type '{message.GetType().Name}' to a '{typeof(TIntegrationMessage).Name}'.");
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