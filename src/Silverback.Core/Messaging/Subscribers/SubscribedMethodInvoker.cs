// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers.ArgumentResolvers;
using Silverback.Messaging.Subscribers.ReturnValueHandlers;
using Silverback.Util;

namespace Silverback.Messaging.Subscribers;

internal static class SubscribedMethodInvoker
{
    public static async ValueTask<MethodInvocationResult> InvokeAsync(
        SubscribedMethod subscribedMethod,
        object message,
        IServiceProvider serviceProvider,
        ExecutionFlow executionFlow)
    {
        if (IsFiltered(subscribedMethod.Options.Filters, message))
            return MethodInvocationResult.NotInvoked;

        object?[] arguments = GetArgumentValuesArray(subscribedMethod, serviceProvider);

        object? returnValue;

        switch (subscribedMethod.MessageArgumentResolver)
        {
            case ISingleMessageArgumentResolver singleResolver:
                returnValue = await InvokeWithSingleMessageAsync(
                    message,
                    subscribedMethod,
                    arguments,
                    singleResolver,
                    serviceProvider,
                    executionFlow).ConfigureAwait(false);
                break;
            case IStreamEnumerableMessageArgumentResolver streamEnumerableResolver:
                returnValue = InvokeWithStreamEnumerable(
                    (IMessageStreamProvider)message,
                    subscribedMethod,
                    arguments,
                    streamEnumerableResolver,
                    serviceProvider);

                break;
            default:
                throw new SubscribedMethodInvocationException(
                    $"The message argument resolver ({subscribedMethod.MessageArgumentResolver}) " +
                    "must implement either ISingleMessageArgumentResolver, IEnumerableMessageArgumentResolver " +
                    "or IStreamEnumerableMessageArgumentResolver.");
        }

        if (returnValue == null)
            return MethodInvocationResult.Invoked;

        bool returnValueWasHandled =
            await serviceProvider
                .GetRequiredService<ReturnValueHandlerService>()
                .HandleReturnValuesAsync(returnValue, executionFlow)
                .ConfigureAwait(false);

        if (returnValueWasHandled)
            return MethodInvocationResult.Invoked;

        return MethodInvocationResult.WithReturnValue(returnValue);
    }

    private static bool IsFiltered(IReadOnlyCollection<IMessageFilter> filters, object message) =>
        filters.Count != 0 && !filters.All(filter => filter.MustProcess(message));

    private static object?[] GetArgumentValuesArray(
        SubscribedMethod method,
        IServiceProvider serviceProvider)
    {
        object?[] values = new object?[method.Parameters.Count];

        for (int i = 1; i < method.Parameters.Count; i++)
        {
            Type parameterType = method.Parameters[i].ParameterType;

            values[i] = method.AdditionalArgumentsResolvers[i - 1]
                .GetValue(parameterType, serviceProvider);
        }

        return values;
    }

    private static ValueTask<object?> InvokeWithSingleMessageAsync(
        object message,
        SubscribedMethod subscribedMethod,
        object?[] arguments,
        ISingleMessageArgumentResolver singleResolver,
        IServiceProvider serviceProvider,
        ExecutionFlow executionFlow)
    {
        message = UnwrapEnvelopeIfNeeded(message, subscribedMethod);

        object target = subscribedMethod.ResolveTargetType(serviceProvider);
        arguments[0] = singleResolver.GetValue(message);
        return InvokeWithActivityAsync(subscribedMethod, target, arguments, executionFlow);
    }

    private static object InvokeWithStreamEnumerable(
        IMessageStreamProvider messageStreamProvider,
        SubscribedMethod subscribedMethod,
        object?[] arguments,
        IStreamEnumerableMessageArgumentResolver streamEnumerableResolver,
        IServiceProvider serviceProvider)
    {
        object target = subscribedMethod.ResolveTargetType(serviceProvider);

        ILazyArgumentValue lazyStream = streamEnumerableResolver.GetValue(
            messageStreamProvider,
            subscribedMethod.MessageType,
            subscribedMethod.Options.Filters);

        return Task.Run(
            async () =>
            {
                try
                {
                    await lazyStream.WaitUntilCreatedAsync().ConfigureAwait(false);

                    arguments[0] = lazyStream.Value;
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                await InvokeWithActivityWithoutBlockingAsync(subscribedMethod, target, arguments).ConfigureAwait(false);
            });
    }

    private static object UnwrapEnvelopeIfNeeded(object message, SubscribedMethod subscribedMethod) =>
        !typeof(IEnvelope).IsAssignableFrom(subscribedMethod.MessageType) &&
        message is IEnvelope envelope &&
        subscribedMethod.MessageType.IsInstanceOfType(envelope.Message)
            ? envelope.Message ?? throw new InvalidOperationException("The envelope message is null.")
            : message;

    private static ValueTask<object?> InvokeWithActivityAsync(
        SubscribedMethod subscribedMethod,
        object target,
        object?[] arguments,
        ExecutionFlow executionFlow) =>
        executionFlow == ExecutionFlow.Async
            ? InvokeWithActivityAsync(subscribedMethod, target, arguments)
            : ValueTaskFactory.FromResult(InvokeWithActivitySync(subscribedMethod, target, arguments));

    private static Task InvokeWithActivityWithoutBlockingAsync(SubscribedMethod subscribedMethod, object target, object?[] arguments) =>
        Task.Run(() => InvokeWithActivityAsync(subscribedMethod, target, arguments).AsTask());

    private static async ValueTask<object?> InvokeWithActivityAsync(SubscribedMethod subscribedMethod, object target, object?[] arguments) =>
        await subscribedMethod.ReturnType.AwaitAndUnwrapResultAsync(InvokeWithActivity(subscribedMethod.MethodInfo, target, arguments)).ConfigureAwait(false);

    private static object? InvokeWithActivitySync(SubscribedMethod subscribedMethod, object target, object?[] arguments) =>
        subscribedMethod.ReturnType.AwaitAndUnwrapResult(InvokeWithActivity(subscribedMethod.MethodInfo, target, arguments));

    private static object? InvokeWithActivity(MethodInfo methodInfo, object? target, object?[] arguments)
    {
        using Activity? activity = ActivitySources.StartInvokeSubscriberActivity(methodInfo);
        return methodInfo.Invoke(target, arguments);
    }
}
