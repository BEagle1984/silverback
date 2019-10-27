// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Subscribers.ArgumentResolvers;
using Silverback.Messaging.Subscribers.ReturnValueHandlers;
using Silverback.Util;

namespace Silverback.Messaging.Subscribers
{
    public class SubscribedMethodInvoker
    {
        private readonly ArgumentsResolver _argumentsResolver;
        private readonly ReturnValueHandler _returnValueHandler;
        private readonly IServiceProvider _serviceProvider;

        public SubscribedMethodInvoker(ArgumentsResolver argumentsResolver, ReturnValueHandler returnValueHandler, IServiceProvider serviceProvider)
        {
            _argumentsResolver = argumentsResolver ?? throw new ArgumentNullException(nameof(argumentsResolver));
            _returnValueHandler = returnValueHandler ?? throw new ArgumentNullException(nameof(returnValueHandler));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public async Task<IEnumerable<object>> Invoke(SubscribedMethod method, IEnumerable<object> messages, bool executeAsync)
        {
            var (messageArgumentResolver, messageType) = _argumentsResolver.GetMessageArgumentResolver(method);

            if (messageArgumentResolver == null)
                return Enumerable.Empty<object>();

            messages = messages.OfType(messageType);

            if (!messages.Any())
                return Enumerable.Empty<object>();

            var target = method.ResolveTargetType(_serviceProvider);
            var parameterValues = GetShiftedParameterValuesArray(method);

            IEnumerable<object> returnValues;

            switch (messageArgumentResolver)
            {
                case ISingleMessageArgumentResolver singleResolver:
                    returnValues = (await messages
                            .OfType(messageType)
                            .SelectAsync(
                                message =>
                                {
                                    parameterValues[0] = singleResolver.GetValue(message);
                                    return Invoke(target, method, parameterValues, executeAsync);
                                },
                                method.IsParallel,
                                method.MaxDegreeOfParallelism))
                        .ToList();
                    break;
                case IEnumerableMessageArgumentResolver enumerableResolver:
                    parameterValues[0] = enumerableResolver.GetValue(messages, messageType);

                    returnValues = new[] {await Invoke(target, method, parameterValues, executeAsync)};
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return await _returnValueHandler.HandleReturnValues(returnValues, executeAsync);
        }

        private object[] GetShiftedParameterValuesArray(SubscribedMethod methodInfo) =>
            new object[1].Concat(
                    _argumentsResolver.GetAdditionalParameterValues(methodInfo))
                .ToArray();
        
        private Task<object> Invoke(object target, SubscribedMethod method, object[] parameters, bool executeAsync) =>
            executeAsync 
                ? InvokeAsync(target, method, parameters) 
                : Task.FromResult(InvokeSync(target, method, parameters));

        private object InvokeSync(object target, SubscribedMethod method, object[] parameters)
        {
            if (!method.MethodInfo.ReturnsTask())
                return method.MethodInfo.Invoke(target, parameters);

            return AsyncHelper.RunSynchronously(() =>
            {
                var result = (Task)method.MethodInfo.Invoke(target, parameters);
                return result.GetReturnValue();
            });
        }

        private Task<object> InvokeAsync(object target, SubscribedMethod method, object[] parameters)
        {
            if (!method.MethodInfo.ReturnsTask())
                return Task.Run(() => method.MethodInfo.Invoke(target, parameters));

            var result = method.MethodInfo.Invoke(target, parameters);
            return ((Task) result).GetReturnValue();
        }
    }
}