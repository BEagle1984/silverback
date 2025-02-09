// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
            bool executeAsync,
            CancellationToken cancellationToken = default)
        {
            if (IsFiltered(subscribedMethod.Options.Filters, message))
                return MethodInvocationResult.NotInvoked;

            var parameters = GetParameterTypeValuesArray(subscribedMethod, serviceProvider);

            object? returnValue;

            switch (subscribedMethod.MessageArgumentResolver)
            {
                case ISingleMessageArgumentResolver singleResolver:
                    returnValue = await InvokeWithSingleMessageAsync(
                        message,
                        subscribedMethod,
                        parameters,
                        singleResolver,
                        serviceProvider,
                        executeAsync,
                        cancellationToken).ConfigureAwait(false);
                    break;
                case IStreamEnumerableMessageArgumentResolver streamEnumerableResolver:
                    returnValue = InvokeWithStreamEnumerable(
                        (IMessageStreamProvider)message,
                        subscribedMethod,
                        parameters,
                        streamEnumerableResolver,
                        serviceProvider,
                        cancellationToken);

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
                    .HandleReturnValuesAsync(returnValue, executeAsync, cancellationToken)
                    .ConfigureAwait(false);

            if (returnValueWasHandled)
                return MethodInvocationResult.Invoked;

            return MethodInvocationResult.WithReturnValue(returnValue);
        }

        private static bool IsFiltered(IReadOnlyCollection<IMessageFilter> filters, object message) =>
            filters.Count != 0 && !filters.All(filter => filter.MustProcess(message));

        private static ParameterTypeValue[] GetParameterTypeValuesArray(
            SubscribedMethod method,
            IServiceProvider serviceProvider)
        {
            var values = new ParameterTypeValue[method.Parameters.Count];

            for (int i = 1; i < method.Parameters.Count; i++)
            {
                var parameterType = method.Parameters[i].ParameterType;

                values[i] = new ParameterTypeValue
                {
                    Type = parameterType,
                    Value = method.AdditionalArgumentsResolvers[i - 1]
                        .GetValue(parameterType, serviceProvider)
                };
            }

            return values;
        }

        private static Task<object?> InvokeWithSingleMessageAsync(
            object message,
            SubscribedMethod subscribedMethod,
            ParameterTypeValue[] parameters,
            ISingleMessageArgumentResolver singleResolver,
            IServiceProvider serviceProvider,
            bool executeAsync,
            CancellationToken cancellationToken)
        {
            message = UnwrapEnvelopeIfNeeded(message, subscribedMethod);

            var target = subscribedMethod.ResolveTargetType(serviceProvider);

            return subscribedMethod.MethodInfo.InvokeWithActivityAsync(target, GetArgumentValuesArray(singleResolver.GetValue(message), parameters, cancellationToken), executeAsync);
        }

        private static object?[] GetArgumentValuesArray(
            object? messageValue,
            ParameterTypeValue[] parameters,
            CancellationToken cancellationToken)
        {
            var arguments = new object?[parameters.Length];
            arguments[0] = messageValue;
            for (int i = 1; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                if (parameter.Type == typeof(CancellationToken))
                {
                    arguments[i] = cancellationToken;
                    continue;
                }

                arguments[i] = parameter.Value;
            }

            return arguments;
        }

        private static object InvokeWithStreamEnumerable(
            IMessageStreamProvider messageStreamProvider,
            SubscribedMethod subscribedMethod,
            ParameterTypeValue[] parameters,
            IStreamEnumerableMessageArgumentResolver streamEnumerableResolver,
            IServiceProvider serviceProvider,
            CancellationToken cancellationToken)
        {
            var target = subscribedMethod.ResolveTargetType(serviceProvider);

            var lazyStream = streamEnumerableResolver.GetValue(
                messageStreamProvider,
                subscribedMethod.MessageType,
                subscribedMethod.Options.Filters);

            return Task.Run(
                async () =>
                {
                    object?[] arguments;
                    try
                    {
                        await lazyStream.WaitUntilCreatedAsync().ConfigureAwait(false);

                        arguments = GetArgumentValuesArray(lazyStream.Value, parameters, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }

                    await subscribedMethod.MethodInfo.InvokeWithActivityWithoutBlockingAsync(
                            target,
                            arguments)
                        .ConfigureAwait(false);
                },
                cancellationToken);
        }

        private static object UnwrapEnvelopeIfNeeded(object message, SubscribedMethod subscribedMethod) =>
            !typeof(IEnvelope).IsAssignableFrom(subscribedMethod.MessageType) &&
            message is IEnvelope envelope &&
            subscribedMethod.MessageType.IsInstanceOfType(envelope.Message)
                ? envelope.Message ?? throw new InvalidOperationException("The envelope message is null.")
                : message;

        private sealed class ParameterTypeValue
        {
            public Type Type { get; init; } = null!;

            public object? Value { get; init; }
        }
    }
}
