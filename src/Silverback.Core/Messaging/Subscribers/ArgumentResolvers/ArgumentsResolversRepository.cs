// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Silverback.Messaging.Subscribers.ArgumentResolvers;

internal sealed class ArgumentsResolversRepository
{
    private readonly IEnumerable<IArgumentResolver> _argumentResolvers;

    public ArgumentsResolversRepository(IEnumerable<IArgumentResolver> argumentResolvers)
    {
        // Revert the resolvers order, to give priority to the ones added after the
        // default ones.
        _argumentResolvers = argumentResolvers.Reverse();
    }

    public (IMessageArgumentResolver Resolver, Type MessageType) GetMessageArgumentResolver(SubscribedMethod method)
    {
        ParameterInfo parameterInfo = method.Parameters[0];
        IMessageArgumentResolver resolver = GetMessageArgumentResolver(parameterInfo, method.MethodInfo);

        return (resolver, resolver.GetMessageType(parameterInfo.ParameterType));
    }

    public IEnumerable<IAdditionalArgumentResolver> GetAdditionalArgumentsResolvers(SubscribedMethod method) =>
        method.Parameters.Skip(1).Select(parameterInfo => GetAdditionalArgumentResolver(parameterInfo, method.MethodInfo));

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
        TResolver? resolver = _argumentResolvers
            .OfType<TResolver>()
            .FirstOrDefault(r => r.CanResolve(parameterInfo.ParameterType));

        if (resolver == null)
        {
            string errorMessage = $"No resolver could be found for argument '{parameterInfo.Name}' " +
                                  $"of type {parameterInfo.ParameterType.FullName}. " +
                                  "Please note that the message (or the enumerable, collection or stream) must " +
                                  "always be the first argument of the subscribing method.";

            throw new SubscribedMethodInvocationException(methodInfo, errorMessage);
        }

        return resolver;
    }
}
