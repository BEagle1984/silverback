// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Messaging.Publishing
{
    public class Publisher : IPublisher
    {
        private readonly ILogger<Publisher> _logger;
        private readonly SubscribedMethodProvider _subscribedMethodsProvider;

        public Publisher(IServiceProvider serviceProvider, ILogger<Publisher> logger)
        {
            _subscribedMethodsProvider = new SubscribedMethodProvider(serviceProvider);
            _logger = logger;
        }

        public void Publish(IMessage message) => Publish(message, false).Wait();

        public Task PublishAsync(IMessage message) => Publish(message, true);

        public IEnumerable<TResult> Publish<TResult>(IMessage message) => Publish(message, false).Result.Cast<TResult>();

        public async Task<IEnumerable<TResult>> PublishAsync<TResult>(IMessage message) => (await Publish(message, true)).Cast<TResult>();

        // TODO: Test recursion
        private async Task<IEnumerable<object>> Publish(IMessage message, bool executeAsync)
        {
            if (message == null) return Enumerable.Empty<object>();

            _logger.LogTrace("Publishing message of type '{messageType}'...", message.GetType().FullName);

            var resultsCollection = new List<object>();

            foreach (var method in _subscribedMethodsProvider.GetSubscribedMethods(message))
            {
                await InvokeSubscribedMethodAndCollectResult(method, message, executeAsync, resultsCollection);
            }

            return resultsCollection.Where(r => r != null);
        }

        private async Task InvokeSubscribedMethodAndCollectResult(SubscribedMethod method, IMessage message, bool executeAsync, List<object> resultsCollection)
        {
            var methodResult = await SubscribedMethodInvoker.InvokeAndGetResult(method, message, executeAsync);

            if (!await PublishReturnedMessages(methodResult, executeAsync, resultsCollection));
            {
                resultsCollection.Add(methodResult);
            }
        }

        private async Task<bool> PublishReturnedMessages(object methodResult, bool executeAsync, List<object> resultsCollection)
        {
            if (methodResult is IMessage returnMessage)
            {
                resultsCollection.AddRange(await Publish(returnMessage, executeAsync));

                return true;
            }
            else if (methodResult is IEnumerable<IMessage> returnMessages)
            {
                foreach (var returnMessage2 in returnMessages)
                    resultsCollection.AddRange(await Publish(returnMessage2, executeAsync));

                return true;
            }

            return false;
        }
    }
}