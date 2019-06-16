// Copyright (c) 2018-2019 Sergio Aquilini
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
    // TODO: Test
    public class SubscribedMethodInvoker
    {
        private readonly SubscribedMethodArgumentsResolver _argumentsResolver;
        private readonly ReturnValueHandler _returnValueHandler;

        public SubscribedMethodInvoker(SubscribedMethodArgumentsResolver argumentsResolver,
            ReturnValueHandler returnValueHandler)
        {
            _argumentsResolver = argumentsResolver ?? throw new ArgumentNullException(nameof(argumentsResolver));
            _returnValueHandler = returnValueHandler ?? throw new ArgumentNullException(nameof(returnValueHandler));
        }

        public async Task<IEnumerable<object>> Invoke(SubscribedMethod method, IEnumerable<object> messages, bool executeAsync)
        {
            var (messageArgumentResolver, messageType) = _argumentsResolver.GetMessageArgumentResolver(method);

            if (messageArgumentResolver == null)
                return Enumerable.Empty<object>();

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
                                    return Invoke(method, parameterValues, executeAsync);
                                },
                                method.Info.IsParallel,
                                method.Info.MaxDegreeOfParallelism))
                        .ToList();
                    break;
                case IEnumerableMessageArgumentResolver enumerableResolver:
                    parameterValues[0] = enumerableResolver.GetValue(messages, messageType);

                    returnValues = new[] {await Invoke(method, parameterValues, executeAsync)};
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return await _returnValueHandler.HandleReturnValues(returnValues, executeAsync);
        }

        private object[] GetShiftedParameterValuesArray(SubscribedMethod method) =>
            new object[1].Concat(
                    _argumentsResolver.GetAdditionalParameterValues(method))
                .ToArray();
        
        private Task<object> Invoke(SubscribedMethod method, object[] parameters, bool executeAsync) =>
            executeAsync 
                ? InvokeAsync(method, parameters) 
                : Task.FromResult(InvokeSync(method, parameters));

        private object InvokeSync(SubscribedMethod method, object[] parameters)
        {
            if (!method.Info.MethodInfo.ReturnsTask())
                return method.Info.MethodInfo.Invoke(method.Target, parameters);

            return AsyncHelper.RunSynchronously<object>(() =>
            {
                var result = (Task)method.Info.MethodInfo.Invoke(method.Target, parameters);
                return result.GetReturnValue();
            });
        }

        private Task<object> InvokeAsync(SubscribedMethod method, object[] parameters)
        {
            if (!method.Info.MethodInfo.ReturnsTask())
                return Task.Run(() => method.Info.MethodInfo.Invoke(method.Target, parameters));

            var result = method.Info.MethodInfo.Invoke(method.Target, parameters);
            return ((Task) result).GetReturnValue();
        }
    }
}