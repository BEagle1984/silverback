// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers.ArgumentResolvers;
using Silverback.Messaging.Subscribers.ReturnValueHandlers;

namespace Silverback.Messaging.Subscribers;

internal static class SubscribedMethodInvoker
{
    public static async ValueTask<MethodInvocationResult> InvokeAsync(
        IPublisher publisher,
        SubscribedMethod subscribedMethod,
        object message,
        IServiceProvider serviceProvider,
        ExecutionFlow executionFlow,
        CancellationToken cancellationToken)
    {
        if (IsFiltered(subscribedMethod.Options.Filters, message))
            return MethodInvocationResult.NotInvoked;

        object?[] arguments = GetArgumentValuesArray(subscribedMethod, serviceProvider, cancellationToken);
        object? returnValue = subscribedMethod.MessageArgumentResolver switch
        {
            ISingleMessageArgumentResolver resolver =>
                await InvokeWithSingleMessageAsync(
                    message,
                    subscribedMethod,
                    arguments,
                    resolver,
                    serviceProvider,
                    executionFlow).ConfigureAwait(false),
            IStreamEnumerableMessageArgumentResolver resolver =>
                InvokeWithStreamEnumerableAsync(
                    (IMessageStreamProvider)message,
                    subscribedMethod,
                    arguments,
                    resolver,
                    serviceProvider),
            _ =>
                throw new SubscribedMethodInvocationException(
                    $"The message argument resolver ({subscribedMethod.MessageArgumentResolver}) " +
                    "must implement either ISingleMessageArgumentResolver, IEnumerableMessageArgumentResolver " +
                    "or IStreamEnumerableMessageArgumentResolver.")
        };

        if (returnValue == null)
            return MethodInvocationResult.Invoked;

        bool returnValueWasHandled =
            await serviceProvider
                .GetRequiredService<ReturnValueHandlerService>()
                .HandleReturnValuesAsync(publisher, returnValue, executionFlow)
                .ConfigureAwait(false);

        if (returnValueWasHandled)
            return MethodInvocationResult.Invoked;

        return MethodInvocationResult.WithReturnValue(returnValue);
    }

    private static bool IsFiltered(IReadOnlyCollection<IMessageFilter> filters, object message) =>
        filters.Count != 0 && !filters.All(filter => filter.MustProcess(message));

    private static object?[] GetArgumentValuesArray(
        SubscribedMethod method,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        object?[] values = new object?[method.Parameters.Count];

        for (int i = 1; i < method.Parameters.Count; i++)
        {
            Type parameterType = method.Parameters[i].ParameterType;

            values[i] = method.AdditionalArgumentsResolvers[i - 1].GetValue(parameterType, serviceProvider, cancellationToken);
        }

        return values;
    }

    private static ValueTask<object?> InvokeWithSingleMessageAsync(
        object? message,
        SubscribedMethod subscribedMethod,
        object?[] arguments,
        ISingleMessageArgumentResolver singleResolver,
        IServiceProvider serviceProvider,
        ExecutionFlow executionFlow)
    {
        message = UnwrapIfNeeded(message, subscribedMethod);

        object target = subscribedMethod.ResolveTargetType(serviceProvider);
        arguments[0] = singleResolver.GetValue(message, subscribedMethod.MessageParameter.ParameterType);
        return InvokeWithActivityAsync(subscribedMethod, target, arguments, executionFlow);
    }

    private static Task InvokeWithStreamEnumerableAsync(
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

    private static object? UnwrapIfNeeded(object? message, SubscribedMethod subscribedMethod) =>
        !typeof(IEnvelope).IsAssignableFrom(subscribedMethod.MessageType) &&
        message is IEnvelope envelope && subscribedMethod.MessageType.IsAssignableFrom(envelope.MessageType)
            ? envelope.Message
            : message;

    private static Task<object?> InvokeWithActivityWithoutBlockingAsync(SubscribedMethod subscribedMethod, object target, object?[] arguments) =>
        Task.Run(() => InvokeWithActivityAsync(subscribedMethod, target, arguments).AsTask());

    private static ValueTask<object?> InvokeWithActivityAsync(
        SubscribedMethod subscribedMethod,
        object target,
        object?[] arguments,
        ExecutionFlow executionFlow) =>
        executionFlow == ExecutionFlow.Async
            ? InvokeWithActivityAsync(subscribedMethod, target, arguments)
            : ValueTask.FromResult(InvokeWithActivitySync(subscribedMethod, target, arguments));

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
