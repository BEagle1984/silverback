// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers.ArgumentResolvers;
using Silverback.Messaging.Subscribers.ReturnValueHandlers;
using Silverback.Util;

namespace Silverback.Messaging.Subscribers
{
    internal class SubscribedMethodInvoker
    {
        private readonly ReturnValueHandlerService _returnValueHandler;

        private readonly IServiceProvider _serviceProvider;

        public SubscribedMethodInvoker(
            ReturnValueHandlerService returnValueHandler,
            IServiceProvider serviceProvider)
        {
            _returnValueHandler = Check.NotNull(returnValueHandler, nameof(returnValueHandler));
            _serviceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
        }

        public async Task<MethodInvocationResult> InvokeAsync(
            SubscribedMethod subscribedMethod,
            IReadOnlyCollection<object> messages,
            bool executeAsync)
        {
            messages = ApplyFilters(subscribedMethod.Filters, messages);

            if (messages.Count == 0)
                return new MethodInvocationResult(messages);

            var arguments = GetArgumentValuesArray(subscribedMethod);

            IReadOnlyCollection<object?>? returnValues;

            switch (subscribedMethod.MessageArgumentResolver)
            {
                case ISingleMessageArgumentResolver singleResolver:
                    (messages, returnValues) = await InvokeForEachMessageAsync(
                        messages,
                        subscribedMethod,
                        arguments,
                        singleResolver,
                        executeAsync).ConfigureAwait(false);
                    break;
                case IEnumerableMessageArgumentResolver enumerableResolver:
                    (messages, returnValues) = await InvokeWithCollectionAsync(
                        messages,
                        subscribedMethod,
                        arguments,
                        enumerableResolver,
                        executeAsync).ConfigureAwait(false);
                    break;
                case IStreamEnumerableMessageArgumentResolver streamEnumerableResolver:
                    (messages, returnValues) = InvokeWithStreamEnumerable(
                        messages,
                        subscribedMethod,
                        arguments,
                        streamEnumerableResolver);

                    break;
                default:
                    throw new SubscribedMethodInvocationException(
                        $"The message argument resolver ({subscribedMethod.MessageArgumentResolver}) " +
                        "must implement either ISingleMessageArgumentResolver, IEnumerableMessageArgumentResolver " +
                        "or IStreamEnumerableMessageArgumentResolver.");
            }

            if (returnValues == null)
                return new MethodInvocationResult(messages);

            var unhandledReturnValues =
                await _returnValueHandler.HandleReturnValuesAsync(returnValues, executeAsync)
                    .ConfigureAwait(false);

            return new MethodInvocationResult(messages, unhandledReturnValues);
        }

        private static IReadOnlyCollection<object> ApplyFilters(
            IReadOnlyCollection<IMessageFilter> filters,
            IReadOnlyCollection<object> messages)
        {
            if (filters.Count == 0)
                return messages;

            return messages.Where(message => filters.All(filter => filter.MustProcess(message))).ToList();
        }

        private object?[] GetArgumentValuesArray(SubscribedMethod method)
        {
            var values = new object?[method.Parameters.Count];

            for (int i = 1; i < method.Parameters.Count; i++)
            {
                var parameterType = method.Parameters[i].ParameterType;

                values[i] = method.AdditionalArgumentsResolvers[i - 1].GetValue(parameterType, _serviceProvider);
            }

            return values;
        }

        private async Task<(IReadOnlyCollection<object> messages, IReadOnlyCollection<object?>? returnValues)>
            InvokeForEachMessageAsync(
                IReadOnlyCollection<object> messages,
                SubscribedMethod subscribedMethod,
                object?[] arguments,
                ISingleMessageArgumentResolver singleResolver,
                bool executeAsync)
        {
            messages = FilterMessagesAndUnwrapEnvelopes(messages, subscribedMethod).ToList();

            if (messages.Count == 0)
                return (messages, null);

            var target = subscribedMethod.ResolveTargetType(_serviceProvider);

            var returnValues = await messages
                .SelectAsync(
                    message =>
                    {
                        arguments[0] = singleResolver.GetValue(message);
                        return InvokeAsync(target, subscribedMethod.MethodInfo, arguments, executeAsync);
                    },
                    subscribedMethod.IsParallel,
                    subscribedMethod.MaxDegreeOfParallelism)
                .ConfigureAwait(false);

            return (messages, returnValues.ToList());
        }

        private async Task<(IReadOnlyCollection<object> messages, IReadOnlyCollection<object?>? returnValues)>
            InvokeWithCollectionAsync(
                IReadOnlyCollection<object> messages,
                SubscribedMethod subscribedMethod,
                object?[] arguments,
                IEnumerableMessageArgumentResolver enumerableResolver,
                bool executeAsync)
        {
            if (messages.Count == 1 && messages.AsReadOnlyList()[0] is IMessageStreamProvider streamProvider &&
                streamProvider.AllowSubscribeAsEnumerable &&
                enumerableResolver is IStreamEnumerableMessageArgumentResolver resolverFromStreamProvider)
            {
                return InvokeWithStreamEnumerable(
                    messages,
                    subscribedMethod,
                    arguments,
                    resolverFromStreamProvider);
            }

            messages = FilterMessagesAndUnwrapEnvelopes(messages, subscribedMethod).ToList();

            if (messages.Count == 0)
                return (messages, null);

            var target = subscribedMethod.ResolveTargetType(_serviceProvider);

            arguments[0] = enumerableResolver.GetValue(messages, subscribedMethod.MessageType);

            var returnValue = await InvokeAsync(target, subscribedMethod.MethodInfo, arguments, executeAsync)
                .ConfigureAwait(false);

            return (messages, new[] { returnValue });
        }

        private (IReadOnlyCollection<object> messages, IReadOnlyCollection<object?>? returnValues)
            InvokeWithStreamEnumerable(
                IReadOnlyCollection<object> messages,
                SubscribedMethod subscribedMethod,
                object?[] arguments,
                IStreamEnumerableMessageArgumentResolver streamEnumerableResolver)
        {
            var streamProviders = FilterMessageStreamEnumerableMessages(messages, subscribedMethod).ToArray();

            if (streamProviders.Length == 0)
                return (streamProviders, null);

            var target = subscribedMethod.ResolveTargetType(_serviceProvider);

            var resultTasks = streamProviders
                .Select(
                    streamProvider =>
                    {
                        arguments[0] = streamEnumerableResolver.GetValue(streamProvider, subscribedMethod.MessageType);
                        return InvokeWithoutBlockingAsync(target, subscribedMethod.MethodInfo, arguments);
                    });

            return (messages, resultTasks.ToArray());
        }

        private static IEnumerable<object> FilterMessagesAndUnwrapEnvelopes(
            IEnumerable<object> messages,
            SubscribedMethod subscribedMethod)
        {
            foreach (var message in messages)
            {
                if (message is IEnvelope envelope && envelope.AutoUnwrap &&
                    subscribedMethod.MessageType.IsInstanceOfType(envelope.Message))
                {
                    yield return envelope.Message!;
                }
                else if (subscribedMethod.MessageType.IsInstanceOfType(message))
                {
                    yield return message;
                }
            }
        }

        private static IEnumerable<IMessageStreamProvider> FilterMessageStreamEnumerableMessages(
            IEnumerable<object> messages,
            SubscribedMethod subscribedMethod)
        {
            foreach (var message in messages)
            {
                if (message is IMessageStreamProvider streamProvider)
                {
                    if (subscribedMethod.MessageArgumentType.IsInstanceOfType(message))
                        yield return streamProvider;

                    // There is no way to properly match the message types in the case of a stream of IEnvelope
                    // and a subscriber that is not handling a stream of envelopes. The envelopes can contain any
                    // type of message (object? Message) and will automatically be unwrapped, filtered and properly
                    // routed by the MessageStreamEnumerable.
                    if (typeof(IEnvelope).IsAssignableFrom(streamProvider.MessageType) &&
                        !typeof(IEnvelope).IsAssignableFrom(subscribedMethod.MessageType))
                        yield return streamProvider;

                    if (streamProvider.MessageType.IsAssignableFrom(subscribedMethod.MessageType))
                        yield return streamProvider;
                }
            }
        }

        private static Task<object?> InvokeAsync(
            object target,
            MethodInfo methodInfo,
            object?[] parameters,
            bool executeAsync) =>
            executeAsync
                ? InvokeAsync(target, methodInfo, parameters)
                : Task.FromResult(InvokeSync(target, methodInfo, parameters));

        private static object? InvokeSync(object target, MethodInfo methodInfo, object?[] parameters) =>
            methodInfo.ReturnsTask()
                ? AsyncHelper.RunSynchronously(
                    () =>
                    {
                        var result = (Task)methodInfo.Invoke(target, parameters);
                        return result.GetReturnValueAsync();
                    })
                : methodInfo.Invoke(target, parameters);

        private static Task<object?> InvokeAsync(object target, MethodInfo methodInfo, object?[] parameters) =>
            methodInfo.ReturnsTask()
                ? ((Task)methodInfo.Invoke(target, parameters)).GetReturnValueAsync()
                : Task.FromResult((object?)methodInfo.Invoke(target, parameters));

        private static Task InvokeWithoutBlockingAsync(
            object target,
            MethodInfo methodInfo,
            object?[] parameters) =>
            methodInfo.ReturnsTask()
                ? (Task)methodInfo.Invoke(target, parameters)
                : Task.Run(() => (object?)methodInfo.Invoke(target, parameters));
    }
}
