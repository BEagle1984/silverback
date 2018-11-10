using System;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Messaging.Integration
{
    /// <summary>
    /// Translates an <see cref="IMessage"/> into another type of <see cref="IMessage"/>. Mostly used to convert
    /// the internal <see cref="IMessage"/> to <see cref="IIntegrationMessage"/> to be sent over the broker.
    /// </summary>
    public abstract class MessageTranslator<TSource, TDestination>
        where TSource : IMessage
        where TDestination : IMessage
    {
        [Subscribe]
        public void OnNext(TSource message, IBus bus)
        {
            if (bus == null) throw new InvalidOperationException("Not initialized.");

            var logger = bus.GetLoggerFactory().CreateLogger<MessageTranslator<TSource, TDestination>>();

            logger.LogTrace($"Mapping message of type '{message.GetType().Name}' to a '{typeof(TDestination).Name}'.");
            bus.Publish(Map(message));
        }

        protected abstract TDestination Map(TSource source);
    }
}