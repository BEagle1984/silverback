using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Subscribers
{
    public abstract class SubscriberBase<TMessage> : ISubscriber
        where TMessage : IMessage
    {
        private ILogger _logger;

        public Func<TMessage, bool> Filter { get; set; }

        protected SubscriberBase(Func<TMessage, bool> filter = null)
        {
            Filter = filter;
        }

        public virtual void Init(IBus bus)
        {
            _logger = bus.GetLoggerFactory().CreateLogger<SubscriberBase<TMessage>>();
        }

        public void OnNext(IMessage message)
        {
            _logger.LogTrace("Message received (synchronously).");

            if (MustHandle(message, out var typedMessage))
            {
                Handle(typedMessage);
            }
        }

        public Task OnNextAsync(IMessage message)
        {
            _logger.LogTrace("Message received (asynchronously).");

            if (MustHandle(message, out var typedMessage))
            {
                return HandleAsync(typedMessage);
            }

            return Task.CompletedTask;
        }

        protected bool MustHandle(IMessage message, out TMessage typedMessage)
        {
            if (!(message is TMessage))
            {
                _logger.LogTrace($"Discarding message because it doesn't match type '{typeof(TMessage).Name}'.");
                typedMessage = default;
                return false;
            }

            typedMessage = (TMessage)message;

            if (Filter != null && !Filter.Invoke(typedMessage))
            {
                _logger.LogTrace("Discarding message because it was filtered out by the custom filter function.");
                return false;
            }

            _logger.LogDebug($"Processing message of type '{typeof(TMessage).Name}'.");

            return true;
        }
        
        public abstract void Handle(TMessage message);

        public abstract Task HandleAsync(TMessage message);
    }
}