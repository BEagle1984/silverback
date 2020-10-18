// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.Util;

namespace Silverback.Messaging.Publishing
{
    /// <inheritdoc cref="IPublisher" />
    public class Publisher : IPublisher
    {
        private readonly ISilverbackLogger _logger;

        private readonly IServiceProvider _serviceProvider;

        private readonly IBehaviorsProvider _behaviorsProvider;

        private SubscribedMethodInvoker? _methodInvoker;

        private SubscribedMethodsLoader? _methodsLoader;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Publisher" /> class.
        /// </summary>
        /// <param name="behaviorsProvider">
        ///     The <see cref="IBehaviorsProvider" />.
        /// </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> instance to be used to resolve the subscribers.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ISilverbackLogger" />.
        /// </param>
        public Publisher(
            IBehaviorsProvider behaviorsProvider,
            IServiceProvider serviceProvider,
            ISilverbackLogger<Publisher> logger)
        {
            _behaviorsProvider = Check.NotNull(behaviorsProvider, nameof(behaviorsProvider));
            _serviceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
            _logger = Check.NotNull(logger, nameof(logger));
        }

        /// <inheritdoc cref="IPublisher.Publish(object)" />
        public void Publish(object message) => Publish(message, false);

        /// <inheritdoc cref="IPublisher.Publish(object, bool)" />
        public void Publish(object message, bool throwIfUnhandled)
        {
            Check.NotNull(message, nameof(message));

            Publish(new[] { message }, throwIfUnhandled);
        }

        /// <inheritdoc cref="IPublisher.Publish{TResult}(object)" />
        public IReadOnlyCollection<TResult> Publish<TResult>(object message) => Publish<TResult>(message, false);

        /// <inheritdoc cref="IPublisher.Publish{TResult}(object, bool)" />
        public IReadOnlyCollection<TResult> Publish<TResult>(object message, bool throwIfUnhandled)
        {
            Check.NotNull(message, nameof(message));

            return Publish<TResult>(new[] { message }, throwIfUnhandled);
        }

        /// <inheritdoc cref="IPublisher.Publish(IEnumerable{object})" />
        public void Publish(IEnumerable<object> messages) => Publish(messages, false);

        /// <inheritdoc cref="IPublisher.Publish(IEnumerable{object}, bool)" />
        public void Publish(IEnumerable<object> messages, bool throwIfUnhandled) =>
            PublishAsync(messages, throwIfUnhandled, false).Wait();

        /// <inheritdoc cref="IPublisher.Publish{TResult}(IEnumerable{object})" />
        public IReadOnlyCollection<TResult> Publish<TResult>(IEnumerable<object> messages) =>
            Publish<TResult>(messages, true);

        /// <inheritdoc cref="IPublisher.Publish{TResult}(IEnumerable{object}, bool)" />
        public IReadOnlyCollection<TResult> Publish<TResult>(
            IEnumerable<object> messages,
            bool throwIfUnhandled) =>
            CastResults<TResult>(PublishAsync(messages, throwIfUnhandled, false).Result).ToList();

        /// <inheritdoc cref="IPublisher.PublishAsync(object)" />
        public Task PublishAsync(object message) => PublishAsync(message, false);

        /// <inheritdoc cref="IPublisher.PublishAsync(object, bool)" />
        public Task PublishAsync(object message, bool throwIfUnhandled)
        {
            Check.NotNull(message, nameof(message));

            return PublishAsync(new[] { message }, throwIfUnhandled);
        }

        /// <inheritdoc cref="IPublisher.PublishAsync{TResult}(object)" />
        public Task<IReadOnlyCollection<TResult>> PublishAsync<TResult>(object message) =>
            PublishAsync<TResult>(message, false);

        /// <inheritdoc cref="IPublisher.PublishAsync{TResult}(object, bool)" />
        public async Task<IReadOnlyCollection<TResult>> PublishAsync<TResult>(
            object message,
            bool throwIfUnhandled) =>
            await PublishAsync<TResult>(new[] { message }, throwIfUnhandled).ConfigureAwait(false);

        /// <inheritdoc cref="IPublisher.PublishAsync(IEnumerable{object})" />
        public Task PublishAsync(IEnumerable<object> messages) => PublishAsync(messages, false);

        /// <inheritdoc cref="IPublisher.PublishAsync(IEnumerable{object}, bool)" />
        public Task PublishAsync(IEnumerable<object> messages, bool throwIfUnhandled) =>
            PublishAsync(messages, throwIfUnhandled, true);

        /// <inheritdoc cref="IPublisher.PublishAsync{TResult}(IEnumerable{object})" />
        public Task<IReadOnlyCollection<TResult>> PublishAsync<TResult>(IEnumerable<object> messages) =>
            PublishAsync<TResult>(messages, false);

        /// <inheritdoc cref="IPublisher.PublishAsync{TResult}(IEnumerable{object}, bool)" />
        public async Task<IReadOnlyCollection<TResult>> PublishAsync<TResult>(
            IEnumerable<object> messages,
            bool throwIfUnhandled) =>
            CastResults<TResult>(await PublishAsync(messages, throwIfUnhandled, true).ConfigureAwait(false)).ToList();

        private static Task<IReadOnlyCollection<object>> ExecuteBehaviorsPipelineAsync(
            Stack<IBehavior> behaviors,
            IReadOnlyCollection<object> messages)
        {
            if (behaviors.TryPop(out var nextBehavior))
            {
                return nextBehavior.HandleAsync(
                    messages,
                    nextMessages =>
                        ExecuteBehaviorsPipelineAsync(behaviors, nextMessages));
            }

            return Task.FromResult(messages);
        }

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
                        CoreEventIds.SubscriberResultDiscarded,
                        "Discarding result of type {Type} because it doesn't match the expected return type {ExpectedType}.",
                        result?.GetType().FullName,
                        typeof(TResult).FullName);
                }
            }
        }

        private async Task<IReadOnlyCollection<object?>> PublishAsync(
            IEnumerable<object> messages,
            bool throwIfUnhandled,
            bool executeAsync)
        {
            Check.NotNull(messages, nameof(messages));

            IReadOnlyCollection<object> messagesList = messages.AsReadOnlyCollection();

            if (!messagesList.Any())
                return Array.Empty<object>();

            Check.HasNoNulls(messagesList, nameof(messages));

            messagesList = await ExecuteBehaviorsPipelineAsync(_behaviorsProvider.CreateStack(), messagesList)
                .ConfigureAwait(false);

            var results = await InvokeSubscribedMethodsAsync(messagesList, executeAsync).ConfigureAwait(false);

            bool allMessagesHandled =
                messagesList.All(
                    message => results.Any(
                        result =>
                            result.HandledMessages.Contains(message) ||
                            message is IEnvelope envelope && result.HandledMessages.Contains(envelope.Message)));

            if (!allMessagesHandled && throwIfUnhandled)
            {
                throw new UnhandledMessageException(messagesList);
            }

            return results.SelectMany(result => result.ReturnValues).ToList();
        }

        private async Task<IReadOnlyCollection<MethodInvocationResult>> InvokeSubscribedMethodsAsync(
            IReadOnlyCollection<object> messages,
            bool executeAsync)
        {
            var methods = GetMethodsLoader().GetSubscribedMethods();
            return (await InvokeExclusiveMethodsAsync(messages, methods, executeAsync).ConfigureAwait(false))
                .Union(await InvokeNonExclusiveMethodsAsync(messages, methods, executeAsync).ConfigureAwait(false))
                .ToList();
        }

        private async Task<IReadOnlyCollection<MethodInvocationResult>> InvokeExclusiveMethodsAsync(
            IReadOnlyCollection<object> messages,
            IReadOnlyCollection<SubscribedMethod> methods,
            bool executeAsync) =>
            (await methods
                .Where(method => method.IsExclusive)
                .SelectAsync(
                    method =>
                        GetMethodInvoker().InvokeAsync(method, messages, executeAsync))
                .ConfigureAwait(false))
            .ToList();

        private async Task<IReadOnlyCollection<MethodInvocationResult>> InvokeNonExclusiveMethodsAsync(
            IReadOnlyCollection<object> messages,
            IReadOnlyCollection<SubscribedMethod> methods,
            bool executeAsync) =>
            (await methods
                .Where(method => !method.IsExclusive)
                .ParallelSelectAsync(
                    method =>
                        GetMethodInvoker().InvokeAsync(method, messages, executeAsync))
                .ConfigureAwait(false))
            .ToList();

        private SubscribedMethodInvoker GetMethodInvoker() =>
            _methodInvoker ??= _serviceProvider.GetRequiredService<SubscribedMethodInvoker>();

        private SubscribedMethodsLoader GetMethodsLoader() =>
            _methodsLoader ??= _serviceProvider.GetRequiredService<SubscribedMethodsLoader>();
    }
}
