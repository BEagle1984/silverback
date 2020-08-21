// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers.ArgumentResolvers;
using Silverback.Messaging.Subscribers.ReturnValueHandlers;
using Silverback.Util;

namespace Silverback.Messaging.Subscribers
{
    internal class SubscribedMethodInvoker
    {
        private readonly ArgumentsResolverService _argumentsResolver;

        private readonly ReturnValueHandlerService _returnValueHandler;

        private readonly IServiceProvider _serviceProvider;

        public SubscribedMethodInvoker(
            ArgumentsResolverService argumentsResolver,
            ReturnValueHandlerService returnValueHandler,
            IServiceProvider serviceProvider)
        {
            _argumentsResolver = Check.NotNull(argumentsResolver, nameof(argumentsResolver));
            _returnValueHandler = Check.NotNull(returnValueHandler, nameof(returnValueHandler));
            _serviceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
        }

        public async Task<MethodInvocationResult> Invoke(
            SubscribedMethod subscribedMethod,
            IReadOnlyCollection<object> messages,
            bool executeAsync)
        {
            var (messageArgumentResolver, targetMessageType) =
                _argumentsResolver.GetMessageArgumentResolver(subscribedMethod);

            if (messageArgumentResolver == null)
                return MethodInvocationResult.Empty;

            messages = UnwrapEnvelopesAndFilterMessages(messages, targetMessageType, subscribedMethod);

            if (!messages.Any())
                return MethodInvocationResult.Empty;

            var target = subscribedMethod.ResolveTargetType(_serviceProvider);
            var parameterValues = GetShiftedParameterValuesArray(subscribedMethod);

            IReadOnlyCollection<object?> returnValues;

            switch (messageArgumentResolver)
            {
                case ISingleMessageArgumentResolver singleResolver:
                    returnValues = (await messages
                            .SelectAsync(
                                message =>
                                {
                                    parameterValues[0] = singleResolver.GetValue(message);
                                    return Invoke(target, subscribedMethod, parameterValues, executeAsync);
                                },
                                subscribedMethod.IsParallel,
                                subscribedMethod.MaxDegreeOfParallelism)
                            .ConfigureAwait(false))
                        .ToList();
                    break;
                case IEnumerableMessageArgumentResolver enumerableResolver:
                    parameterValues[0] = enumerableResolver.GetValue(messages, targetMessageType);

                    returnValues = new[]
                    {
                        await Invoke(target, subscribedMethod, parameterValues, executeAsync)
                            .ConfigureAwait(false)
                    };
                    break;
                default:
                    throw new SubscribedMethodInvocationException(
                        $"The message argument resolver ({messageArgumentResolver}) must implement either " +
                        "ISingleMessageArgumentResolver or IEnumerableMessageArgumentResolver.");
            }

            var unhandledReturnValues =
                await _returnValueHandler.HandleReturnValues(returnValues, executeAsync)
                    .ConfigureAwait(false);

            return new MethodInvocationResult(messages, unhandledReturnValues);
        }

        private static IReadOnlyCollection<object> UnwrapEnvelopesAndFilterMessages(
            IEnumerable<object> messages,
            Type targetMessageType,
            SubscribedMethod subscribedMethod) =>
            UnwrapEnvelopesIfNeeded(
                    ApplyFilters(subscribedMethod.Filters, messages),
                    targetMessageType)
                .OfType(targetMessageType)
                .ToList();

        private static IEnumerable<object> UnwrapEnvelopesIfNeeded(
            IEnumerable<object> messages,
            Type targetMessageType) =>
            typeof(IEnvelope).IsAssignableFrom(targetMessageType)
                ? messages
                : GetUnwrappedMessages(messages);

        private static IEnumerable<object> GetUnwrappedMessages(IEnumerable<object> messages)
        {
            foreach (var message in messages)
            {
                if (message is IEnvelope envelope)
                {
                    if (envelope.AutoUnwrap && envelope.Message != null)
                        yield return envelope.Message;
                }
                else
                {
                    yield return message;
                }
            }
        }

        private static IEnumerable<object> ApplyFilters(
            IReadOnlyCollection<IMessageFilter> filters,
            IEnumerable<object> messages) =>
            messages.Where(message => filters.All(filter => filter.MustProcess(message)));

        private static Task<object?> Invoke(
            object target,
            SubscribedMethod method,
            object?[] parameters,
            bool executeAsync) =>
            executeAsync
                ? InvokeAsync(target, method, parameters)
                : Task.FromResult(InvokeSync(target, method, parameters));

        private static object? InvokeSync(object target, SubscribedMethod method, object?[] parameters)
        {
            if (!method.MethodInfo.ReturnsTask())
                return method.MethodInfo.Invoke(target, parameters);

            return AsyncHelper.RunSynchronously(
                () =>
                {
                    var result = (Task)method.MethodInfo.Invoke(target, parameters);
                    return result.GetReturnValue();
                });
        }

        private static Task<object?> InvokeAsync(object target, SubscribedMethod method, object?[] parameters)
        {
            if (!method.MethodInfo.ReturnsTask())
                return Task.FromResult((object?)method.MethodInfo.Invoke(target, parameters));

            var result = method.MethodInfo.Invoke(target, parameters);
            return ((Task)result).GetReturnValue();
        }

        private object?[] GetShiftedParameterValuesArray(SubscribedMethod methodInfo) =>
            new object[1].Concat(_argumentsResolver.GetAdditionalParameterValues(methodInfo))
                .ToArray();
    }
}
