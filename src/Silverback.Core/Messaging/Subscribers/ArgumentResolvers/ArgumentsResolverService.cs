// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Silverback.Messaging.Subscribers.ArgumentResolvers
{
    /// <summary>
    ///     Calls the registered <see cref="IArgumentResolver"/>'s.
    /// </summary>
    internal class ArgumentsResolverService
    {
        private readonly IEnumerable<IArgumentResolver> _argumentResolvers;

        public ArgumentsResolverService(IEnumerable<IArgumentResolver> argumentResolvers)
        {
            // Revert the resolvers order, to give priority to the ones added after the
            // default ones.
            _argumentResolvers = argumentResolvers.Reverse();
        }

        public (IMessageArgumentResolver resolver, Type messageType) GetMessageArgumentResolver(
            SubscribedMethod methodInfo)
        {
            IMessageArgumentResolver resolver = GetMessageArgumentResolver(
                methodInfo.Parameters[0],
                methodInfo.MethodInfo);

            return (resolver, resolver.GetMessageType(methodInfo.Parameters[0].ParameterType));
        }

        public IEnumerable<object?> GetAdditionalParameterValues(SubscribedMethod methodInfo)
        {
            for (int i = 1; i < methodInfo.Parameters.Count; i++)
            {
                var parameterInfo = methodInfo.Parameters[i];

                yield return GetAdditionalArgumentResolver(parameterInfo, methodInfo.MethodInfo)
                    .GetValue(parameterInfo.ParameterType);
            }
        }

        private IMessageArgumentResolver GetMessageArgumentResolver(
            ParameterInfo parameterInfo,
            MethodInfo methodInfo) =>
            GetArgumentResolver<IMessageArgumentResolver>(parameterInfo, methodInfo);

        private IAdditionalArgumentResolver GetAdditionalArgumentResolver(
            ParameterInfo parameterInfo,
            MethodInfo methodInfo) =>
            GetArgumentResolver<IAdditionalArgumentResolver>(parameterInfo, methodInfo);

        private TResolver GetArgumentResolver<TResolver>(ParameterInfo parameterInfo, MethodInfo methodInfo)
            where TResolver : IArgumentResolver
        {
            var resolver = _argumentResolvers
                .OfType<TResolver>()
                .FirstOrDefault(r => r.CanResolve(parameterInfo.ParameterType));

            if (resolver == null)
            {
                var errorMessage = $"No resolver could be found for argument '{parameterInfo.Name}' " +
                                   $"of type {parameterInfo.ParameterType.FullName}.";

                throw new SubscribedMethodInvocationException(methodInfo, errorMessage);
            }

            return resolver;
        }
    }
}
