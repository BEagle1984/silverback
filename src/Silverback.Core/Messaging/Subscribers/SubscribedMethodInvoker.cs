// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers.ArgumentResolvers;
using Silverback.Messaging.Subscribers.ReturnValueHandlers;

namespace Silverback.Messaging.Subscribers
{
    internal static class SubscribedMethodInvoker
    {
        public static async Task<MethodInvocationResult> InvokeAsync(
            SubscribedMethod subscribedMethod,
            object message,
            IServiceProvider serviceProvider,
            bool executeAsync)
        {
            if (IsFiltered(subscribedMethod.Options.Filters, message))
                return MethodInvocationResult.NotInvoked;

            var arguments = GetArgumentValuesArray(subscribedMethod, serviceProvider);

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
                        executeAsync).ConfigureAwait(false);
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
                    .HandleReturnValuesAsync(returnValue, executeAsync)
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
            var values = new object?[method.Parameters.Count];

            for (int i = 1; i < method.Parameters.Count; i++)
            {
                var parameterType = method.Parameters[i].ParameterType;

                values[i] = method.AdditionalArgumentsResolvers[i - 1]
                    .GetValue(parameterType, serviceProvider);
            }

            return values;
        }

        private static Task<object?> InvokeWithSingleMessageAsync(
            object message,
            SubscribedMethod subscribedMethod,
            object?[] arguments,
            ISingleMessageArgumentResolver singleResolver,
            IServiceProvider serviceProvider,
            bool executeAsync)
        {
            message = UnwrapEnvelopeIfNeeded(message, subscribedMethod);

            var target = subscribedMethod.ResolveTargetType(serviceProvider);
            arguments[0] = singleResolver.GetValue(message);
            return subscribedMethod.MethodInfo.InvokeWithActivityAsync(target, arguments, executeAsync);
        }

        private static object InvokeWithStreamEnumerable(
            IMessageStreamProvider messageStreamProvider,
            SubscribedMethod subscribedMethod,
            object?[] arguments,
            IStreamEnumerableMessageArgumentResolver streamEnumerableResolver,
            IServiceProvider serviceProvider)
        {
            var target = subscribedMethod.ResolveTargetType(serviceProvider);

            var lazyStream = streamEnumerableResolver.GetValue(
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

                    await subscribedMethod.MethodInfo.InvokeWithActivityWithoutBlockingAsync(
                            target,
                            arguments)
                        .ConfigureAwait(false);
                });
        }

        private static object UnwrapEnvelopeIfNeeded(object message, SubscribedMethod subscribedMethod) =>
            !typeof(IEnvelope).IsAssignableFrom(subscribedMethod.MessageType) &&
            message is IEnvelope envelope &&
            subscribedMethod.MessageType.IsInstanceOfType(envelope.Message)
                ? envelope.Message ?? throw new InvalidOperationException("The envelope message is null.")
                : message;
    }
}
