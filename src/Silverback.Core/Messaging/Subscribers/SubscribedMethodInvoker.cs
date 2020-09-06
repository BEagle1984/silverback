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

        public async Task<MethodInvocationResult> Invoke(
            SubscribedMethod subscribedMethod,
            IReadOnlyCollection<object> messages,
            bool executeAsync)
        {
            messages = ApplyFilters(subscribedMethod.Filters, messages);

            var arguments = GetArgumentValuesArray(subscribedMethod);

            IReadOnlyCollection<object?>? returnValues = null;

            switch (subscribedMethod.MessageArgumentResolver)
            {
                case ISingleMessageArgumentResolver singleResolver:
                    (messages, returnValues) = await InvokeForEachMessage(
                        messages,
                        subscribedMethod,
                        executeAsync,
                        arguments,
                        singleResolver).ConfigureAwait(false);
                    break;
                case IEnumerableMessageArgumentResolver enumerableResolver:
                    (messages, returnValues) = await InvokeWithCollection(
                        messages,
                        subscribedMethod,
                        executeAsync,
                        arguments,
                        enumerableResolver).ConfigureAwait(false);
                    break;
                case IStreamEnumerableMessageArgumentResolver streamEnumerableResolver:
                    messages = await InvokeWithStreamEnumerable(
                            subscribedMethod,
                            messages,
                            arguments,
                            streamEnumerableResolver)
                        .ConfigureAwait(false);

                    break;
                default:
                    throw new SubscribedMethodInvocationException(
                        $"The message argument resolver ({subscribedMethod.MessageArgumentResolver}) " +
                        $"must implement either ISingleMessageArgumentResolver, IEnumerableMessageArgumentResolver " +
                        $"or IStreamEnumerableMessageArgumentResolver.");
            }

            if (returnValues == null)
                return new MethodInvocationResult(messages);

            var unhandledReturnValues =
                await _returnValueHandler.HandleReturnValues(returnValues, executeAsync)
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

        private async Task<(IReadOnlyCollection<object> messages, IReadOnlyCollection<object?>? returnValues)>
            InvokeForEachMessage(
                IReadOnlyCollection<object> messages,
                SubscribedMethod subscribedMethod,
                bool executeAsync,
                object?[] arguments,
                ISingleMessageArgumentResolver singleResolver)
        {
            messages = FilterMessagesAndUnwrapEnvelopes(messages, subscribedMethod.MessageType).ToArray();

            if (messages.Count == 0)
                return (messages, null);

            var target = subscribedMethod.ResolveTargetType(_serviceProvider);

            var returnValues = await messages
                .SelectAsync(
                    message =>
                    {
                        arguments[0] = singleResolver.GetValue(message);
                        return Invoke(target, subscribedMethod.MethodInfo, arguments, executeAsync);
                    },
                    subscribedMethod.IsParallel,
                    subscribedMethod.MaxDegreeOfParallelism)
                .ConfigureAwait(false);

            return (messages, returnValues.ToList());
        }

        private async Task<(IReadOnlyCollection<object> messages, IReadOnlyCollection<object?>? returnValues)>
            InvokeWithCollection(
                IReadOnlyCollection<object> messages,
                SubscribedMethod subscribedMethod,
                bool executeAsync,
                object?[] arguments,
                IEnumerableMessageArgumentResolver enumerableResolver)
        {
            messages = FilterMessagesAndUnwrapEnvelopes(messages, subscribedMethod.MessageType).ToArray();

            if (messages.Count == 0)
                return (messages, null);

            var target = subscribedMethod.ResolveTargetType(_serviceProvider);

            arguments[0] = enumerableResolver.GetValue(messages, subscribedMethod.MessageType);

            var returnValue = await Invoke(target, subscribedMethod.MethodInfo, arguments, executeAsync)
                .ConfigureAwait(false);

            return (messages, new[] { returnValue });
        }

        private async Task<IReadOnlyCollection<object>> InvokeWithStreamEnumerable(
            SubscribedMethod subscribedMethod,
            IReadOnlyCollection<object> messages,
            object?[] arguments,
            IStreamEnumerableMessageArgumentResolver streamEnumerableResolver)
        {
            messages = messages
                .OfType<IMessageStreamEnumerable<object>>()
                .Where(
                    stream =>
                    {
                        if (subscribedMethod.MessageArgumentType.IsInstanceOfType(stream))
                            return true;

                        // There is no way to properly match the message types in the case of a stream of IEnvelope
                        // and a subscriber that is not handling a stream of envelopes. The envelopes can contain any
                        // type of message (object? Message) and will automatically be unwrapped, filtered and properly
                        // routed by the MessageStreamEnumerable.
                        if (stream is IMessageStreamEnumerable<IEnvelope> &&
                            !typeof(IMessageStreamEnumerable<IEnvelope>).IsAssignableFrom(subscribedMethod.MessageType))
                            return true;

                        if (stream is IMessageStreamEnumerable writableStream &&
                            writableStream.MessageType.IsAssignableFrom(subscribedMethod.MessageType))
                            return true;

                        return false;
                    })
                .ToArray();

            if (messages.Count == 0)
                return messages;

            var target = subscribedMethod.ResolveTargetType(_serviceProvider);

            await messages
                .ForEachAsync(
                    message =>
                    {
                        arguments[0] = streamEnumerableResolver.GetValue(message, subscribedMethod.MessageType);
                        return InvokeWithoutBlockingAsync(target, subscribedMethod.MethodInfo, arguments);
                    })
                .ConfigureAwait(false);

            return messages;
        }

        private static IEnumerable<object> FilterMessagesAndUnwrapEnvelopes(
            IEnumerable<object> messages,
            Type targetMessageType)
        {
            foreach (var message in messages)
            {
                if (message is IEnvelope envelope)
                {
                    if (typeof(IEnvelope).IsAssignableFrom(targetMessageType))
                    {
                        if (targetMessageType.IsInstanceOfType(envelope))
                            yield return envelope;
                    }
                    else if (envelope.AutoUnwrap && targetMessageType.IsInstanceOfType(envelope.Message))
                    {
                        yield return envelope.Message!;
                    }
                }
                else
                {
                    if (targetMessageType.IsInstanceOfType(message))
                        yield return message;
                }
            }
        }

        private static Task<object?> Invoke(
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
                        return result.GetReturnValue();
                    })
                : methodInfo.Invoke(target, parameters);

        private static Task<object?> InvokeAsync(object target, MethodInfo methodInfo, object?[] parameters) =>
            methodInfo.ReturnsTask()
                ? ((Task)methodInfo.Invoke(target, parameters)).GetReturnValue()
                : Task.FromResult((object?)methodInfo.Invoke(target, parameters));

        private static Task InvokeWithoutBlockingAsync(
            object target,
            MethodInfo methodInfo,
            object?[] parameters) =>
            methodInfo.ReturnsTask()
                ? (Task)methodInfo.Invoke(target, parameters)
                : Task.Run(() => (object?)methodInfo.Invoke(target, parameters));

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
    }
}
