// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Messaging.Subscribers;
using Silverback.Util;

namespace Silverback.Messaging.Publishing
{
    /// <inheritdoc cref="IPublisher" />
    public class Publisher : IPublisher
    {
        private readonly ILogger _logger;

        private readonly IServiceProvider _serviceProvider;

        private IReadOnlyCollection<IBehavior>? _behaviors;

        private SubscribedMethodInvoker? _methodInvoker;

        private SubscribedMethodsLoader? _methodsLoader;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Publisher" /> class.
        /// </summary>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> instance to be used to resolve the subscribers.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ILogger" />.
        /// </param>
        public Publisher(IServiceProvider serviceProvider, ILogger<Publisher> logger)
        {
            _serviceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));

            _logger = logger;
        }

        /// <inheritdoc />
        public void Publish(object message)
        {
            Check.NotNull(message, nameof(message));

            Publish(new[] { message });
        }

        /// <inheritdoc />
        public Task PublishAsync(object message)
        {
            Check.NotNull(message, nameof(message));

            return PublishAsync(new[] { message });
        }

        /// <inheritdoc />
        public IReadOnlyCollection<TResult> Publish<TResult>(object message)
        {
            Check.NotNull(message, nameof(message));

            return Publish<TResult>(new[] { message });
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<TResult>> PublishAsync<TResult>(object message) =>
            await PublishAsync<TResult>(new[] { message });

        /// <inheritdoc />
        public void Publish(IEnumerable<object> messages) => Publish(messages, false).Wait();

        /// <inheritdoc />
        public Task PublishAsync(IEnumerable<object> messages) => Publish(messages, true);

        /// <inheritdoc />
        public IReadOnlyCollection<TResult> Publish<TResult>(IEnumerable<object> messages) =>
            CastResults<TResult>(Publish(messages, false).Result).ToList();

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<TResult>> PublishAsync<TResult>(IEnumerable<object> messages) =>
            CastResults<TResult>(await Publish(messages, true)).ToList();

        private IEnumerable<TResult> CastResults<TResult>(IReadOnlyCollection<object?> results)
        {
            foreach (var result in results)
            {
                if (result is TResult castResult)
                {
                    yield return castResult;
                }
                else
                {
                    _logger.LogDebug(
                        EventIds.PublisherDiscardingResult,
                        "Discarding result of type '{Type}' because it doesn't match the expected return type '{ExpectedType}'.",
                        result?.GetType().FullName,
                        typeof(TResult).FullName);
                }
            }
        }

        private async Task<IReadOnlyCollection<object?>> Publish(
            IEnumerable<object> messages,
            bool executeAsync)
        {
            Check.NotNull(messages, nameof(messages));

            var messagesList = messages.ToList(); // TODO: Avoid cloning?

            if (!messagesList.Any())
                return Array.Empty<object>();

            Check.HasNoNulls(messagesList, nameof(messages));

            return await ExecutePipeline(
                GetBehaviors(),
                messagesList,
                async finalMessages =>
                    (await InvokeExclusiveMethods(finalMessages, executeAsync))
                    .Union(await InvokeNonExclusiveMethods(finalMessages, executeAsync))
                    .ToList());
        }

        private static Task<IReadOnlyCollection<object?>> ExecutePipeline(
            IReadOnlyCollection<IBehavior> behaviors,
            IReadOnlyCollection<object> messages,
            Func<IReadOnlyCollection<object>, Task<IReadOnlyCollection<object?>>> finalAction)
        {
            if (behaviors != null && behaviors.Any())
            {
                return behaviors.First().Handle(
                    messages,
                    nextMessages => ExecutePipeline(
                        behaviors.Skip(1).ToList(),
                        nextMessages,
                        finalAction));
            }

            return finalAction(messages);
        }

        private async Task<IReadOnlyCollection<object?>> InvokeExclusiveMethods(
            IReadOnlyCollection<object> messages,
            bool executeAsync) =>
            (await GetMethodsLoader().GetSubscribedMethods()
                .Where(method => method.IsExclusive)
                .SelectManyAsync(
                    method =>
                        GetMethodInvoker().Invoke(method, messages, executeAsync)))
            .ToList();

        private async Task<IReadOnlyCollection<object?>> InvokeNonExclusiveMethods(
            IReadOnlyCollection<object> messages,
            bool executeAsync) =>
            (await GetMethodsLoader().GetSubscribedMethods()
                .Where(method => !method.IsExclusive)
                .ParallelSelectManyAsync(
                    method =>
                        GetMethodInvoker().Invoke(method, messages, executeAsync)))
            .ToList();

        private IReadOnlyCollection<IBehavior> GetBehaviors() =>
            _behaviors ??= _serviceProvider.GetServices<IBehavior>().SortBySortIndex().ToList();

        private SubscribedMethodInvoker GetMethodInvoker() =>
            _methodInvoker ??= _serviceProvider.GetRequiredService<SubscribedMethodInvoker>();

        private SubscribedMethodsLoader GetMethodsLoader() =>
            _methodsLoader ??= _serviceProvider.GetRequiredService<SubscribedMethodsLoader>();
    }
}
