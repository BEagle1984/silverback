// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Subscribers;
using Silverback.Util;

namespace Silverback.Messaging.Publishing
{
    public class Publisher : IPublisher
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;

        private IEnumerable<IBehavior> _behaviors;
        private SubscribedMethodInvoker _methodInvoker;
        private SubscribedMethodsLoader _methodsLoader;

        public Publisher(IServiceProvider serviceProvider, ILogger<Publisher> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            _logger = logger;
        }

        public void Publish(object message) => 
            Publish(new[] { message });

        public Task PublishAsync(object message) =>
            PublishAsync(new[] { message });
    
        public IEnumerable<TResult> Publish<TResult>(object message) => 
            Publish<TResult>(new[] { message });

        public async Task<IEnumerable<TResult>> PublishAsync<TResult>(object message) => 
            await PublishAsync<TResult>(new[] { message });

        public void Publish(IEnumerable<object> messages) => 
            Publish(messages, false).Wait();

        public Task PublishAsync(IEnumerable<object> messages) => Publish(messages, true);

        public IEnumerable<TResult> Publish<TResult>(IEnumerable<object> messages) =>
            CastResults<TResult>(Publish(messages, false).Result);
        
        public async Task<IEnumerable<TResult>> PublishAsync<TResult>(IEnumerable<object> messages) => 
            CastResults<TResult>(await Publish(messages, true));

        private IEnumerable<TResult> CastResults<TResult>(IEnumerable<object> results)
        {
            foreach (var result in results)
            {
                if (result is TResult castResult)
                {
                    yield return castResult;
                }
                else
                { 
                    _logger.LogTrace(
                        $"Discarding result of type {result.GetType().FullName} because it doesn't match " +
                        $"the expected return type {typeof(TResult).FullName}.");
                }
            }
        }

        // TODO: Test recursion
        private async Task<IEnumerable<object>> Publish(IEnumerable<object> messages, bool executeAsync)
        {
            var messagesList = messages?.ToList();

            if (messagesList == null || !messagesList.Any())
                return Enumerable.Empty<object>();

            return await ExecutePipeline(GetBehaviors(), messagesList, async m =>
                (await InvokeExclusiveMethods(m, executeAsync))
                .Union(await InvokeNonExclusiveMethods(m, executeAsync))
                .ToList());
        }

        private Task<IEnumerable<object>> ExecutePipeline(IEnumerable<IBehavior> behaviors, IEnumerable<object> messages, Func<IEnumerable<object>, Task<IEnumerable<object>>> finalAction)
        {
            if (behaviors == null || !behaviors.Any())
                return finalAction(messages);

            return behaviors.First().Handle(messages, m => ExecutePipeline(behaviors.Skip(1), m, finalAction));
        }

        private Task<IEnumerable<object>> InvokeExclusiveMethods(IEnumerable<object> messages, bool executeAsync) =>
            GetMethodsLoader().GetSubscribedMethods()
                .Where(method => method.IsExclusive)
                .SelectManyAsync(method =>
                    GetMethodInvoker().Invoke(method, messages, executeAsync));

        private Task<IEnumerable<object>> InvokeNonExclusiveMethods(IEnumerable<object> messages, bool executeAsync) =>
            GetMethodsLoader().GetSubscribedMethods()
                .Where(method => !method.IsExclusive)
                .ParallelSelectManyAsync(method =>
                    GetMethodInvoker().Invoke(method, messages, executeAsync));

        private IEnumerable<IBehavior> GetBehaviors()
        {
            if (_behaviors == null)
            {
                var behaviors = _serviceProvider.GetServices<IBehavior>();
                var sortedBehaviors = behaviors.OfType<ISortedBehavior>().OrderBy(b => b.SortIndex).ToList();
                behaviors = behaviors.Where(b => !(b is ISortedBehavior)).ToList();

                _behaviors =
                    sortedBehaviors.Where(b => b.SortIndex <= 0)
                        .Union(behaviors)
                        .Union(sortedBehaviors.Where(b => b.SortIndex > 0))
                        .ToList();
            }

            return _behaviors;
        }

        private SubscribedMethodInvoker GetMethodInvoker() =>
            _methodInvoker ??= _serviceProvider.GetRequiredService<SubscribedMethodInvoker>();

        private SubscribedMethodsLoader GetMethodsLoader() =>
            _methodsLoader ??= _serviceProvider.GetRequiredService<SubscribedMethodsLoader>();
    }
}
