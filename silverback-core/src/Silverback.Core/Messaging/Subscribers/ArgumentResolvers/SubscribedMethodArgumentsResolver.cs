// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Silverback.Messaging.Subscribers.ArgumentResolvers
{
    public class SubscribedMethodArgumentsResolver
    {
        private readonly IEnumerable<IArgumentResolver> _argumentResolvers;

        public SubscribedMethodArgumentsResolver(IEnumerable<IArgumentResolver> argumentResolvers)
        {
            _argumentResolvers = argumentResolvers;
        }

        public (IMessageArgumentResolver resolver, Type messageType) GetMessageArgumentResolver(SubscribedMethod method)
        {
            var resolver =GetMessageArgumentResolver(method.Info.Parameters[0], method.Info.MethodInfo);

            return (resolver, resolver.GetMessageType(method.Info.Parameters[0].ParameterType));
        }

        public object[] GetAdditionalParameterValues(SubscribedMethod method) =>
            method.Info.Parameters
                .Skip(1)
                .Select(parameterInfo =>
                    GetAdditionalArgumentResolver(parameterInfo, method.Info.MethodInfo)
                        .GetValue(parameterInfo.ParameterType))
                .ToArray();
        
        private IMessageArgumentResolver GetMessageArgumentResolver(ParameterInfo parameterInfo, MethodInfo methodInfo) =>
            GetArgumentResolver<IMessageArgumentResolver>(parameterInfo, methodInfo);

        private IAdditionalArgumentResolver GetAdditionalArgumentResolver(ParameterInfo parameterInfo, MethodInfo methodInfo) =>
            GetArgumentResolver<IAdditionalArgumentResolver>(parameterInfo, methodInfo);

        private TResolver GetArgumentResolver<TResolver>(ParameterInfo parameterInfo, MethodInfo methodInfo)
            where TResolver : IArgumentResolver
        {
            var resolver = _argumentResolvers
                .OfType<TResolver>()
                .FirstOrDefault(r => r.CanResolve(parameterInfo.ParameterType));

            if (resolver == null)
            {
                throw new SubscribedMethodInvocationException(methodInfo,
                    $"No resolver could be found for argument '{parameterInfo.Name}' of type {parameterInfo.ParameterType.FullName}.");
            }

            return resolver;
        }
    }
}