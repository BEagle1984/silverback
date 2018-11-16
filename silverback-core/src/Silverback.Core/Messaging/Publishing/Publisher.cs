using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.Util;

namespace Silverback.Messaging.Publishing
{
    public class Publisher : IPublisher
    {
        private readonly ILogger<Publisher> _logger;
        private readonly SubscribedMethodProvider _subscribedMethodsProvider;

        public Publisher(IEnumerable<ISubscriber> subscribers, ILogger<Publisher> logger)
        {
            _subscribedMethodsProvider = new SubscribedMethodProvider(subscribers);
            _logger = logger;
        }

        public void Publish(IMessage message)
        {
            if (message == null) return;

            _logger.LogTrace($"Publishing message of type '{message.GetType().FullName}'...");

            _subscribedMethodsProvider.GetSubscribedMethods(message)
                .ForEach(method => InvokeMethodAndRepublishResult(method, message, false).Wait());
        }

        public Task PublishAsync(IMessage message)
        {
            if (message == null) return Task.CompletedTask;

            _logger.LogTrace($"Publishing message of type '{message.GetType().FullName}'...");

            return _subscribedMethodsProvider.GetSubscribedMethods(message)
                .ForEachAsync(method => InvokeMethodAndRepublishResult(method, message, true));
        }

        private async Task InvokeMethodAndRepublishResult(SubscribedMethod method, IMessage message, bool executeAsync)
        {
            var resultMessages = await InvokeMethodAndGetResult(method, message, executeAsync);

            if (executeAsync)
                await resultMessages.ForEachAsync(PublishAsync);
            else
                resultMessages.ForEach(Publish);
        }

        private async Task<IEnumerable<IMessage>> InvokeMethodAndGetResult(SubscribedMethod method, IMessage message, bool executeAsync)
        {
            _logger.LogTrace($"Invoking subscribed method {method.MethodInfo.DeclaringType.FullName}.{method.MethodInfo.Name}...");

            var result = await SubscribedMethodInvoker.InvokeAndGetResult(method, message, executeAsync);

            if (result is IMessage returnMessage)
                return new[] { returnMessage };

            if (result is IEnumerable<IMessage> returnMessages)
                return returnMessages;

            return Enumerable.Empty<IMessage>();
        }
    }
}