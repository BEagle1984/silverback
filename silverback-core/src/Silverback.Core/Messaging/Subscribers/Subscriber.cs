using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Subscribers
{
    public abstract class Subscriber<TMessage> : SubscriberBase<TMessage>
        where TMessage : IMessage
    {
        private readonly ILogger<Subscriber<TMessage>> _logger;
        protected Subscriber(ILogger<Subscriber<TMessage>> logger) : base(logger)
        {
            _logger = logger;
        }

        [Subscribe]
        public void OnNext(TMessage message)
        {
            _logger.LogTrace($"Synchronously processing message of type '{typeof(TMessage).Name}'.");

            if (MustHandle(message))
                Handle(message);
        }

        public abstract void Handle(TMessage message);
    }
}
