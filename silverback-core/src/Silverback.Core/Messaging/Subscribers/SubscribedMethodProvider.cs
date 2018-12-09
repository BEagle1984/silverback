// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Subscribers
{
    internal class SubscribedMethodProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private static readonly ConcurrentDictionary<Type, IEnumerable<SubscribedMethod>> MethodsCache = new ConcurrentDictionary<Type, IEnumerable<SubscribedMethod>>();

        public SubscribedMethodProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IEnumerable<SubscribedMethod> GetSubscribedMethods<TMessage>(TMessage message) =>
            _serviceProvider
                .GetServices<ISubscriber>()
                .SelectMany(GetAnnotatedMethods)
                .Where(method => method.SubscribedMessageType.IsInstanceOfType(message))
                .ToList();

        private IEnumerable<SubscribedMethod> GetAnnotatedMethods(ISubscriber subscriber) =>
            GetAnnotatedMethods(subscriber.GetType())
                .Select(method =>
                {
                    method.Instance = subscriber;
                    return method;
                });

        private IEnumerable<SubscribedMethod> GetAnnotatedMethods(Type type)
            => MethodsCache.GetOrAdd(type, t =>
                t.GetAnnotatedMethods<SubscribeAttribute>()
                    .Select(methodInfo =>
                    {
                        var parameters = methodInfo.GetParameters();
                        return new SubscribedMethod
                        {
                            MethodInfo = methodInfo,
                            Parameters = parameters,
                            SubscribedMessageType = GetMessageParameterType(methodInfo, parameters)
                        };
                    })
                    .ToList());

        private Type GetMessageParameterType(MethodInfo methodInfo, ParameterInfo[] parameters)
        {
            var messageParameters = parameters.Where(p => typeof(IMessage).IsAssignableFrom(p.ParameterType)).ToArray();

            if (messageParameters.Length != 1)
                ThrowMethodSignatureException(methodInfo, "A single parameter of type IMessage or drived type is expected.");

            return parameters.First().ParameterType;
        }

        private void ThrowMethodSignatureException(MethodInfo methodInfo, string message) =>
            throw new SilverbackException(
                $"The method {methodInfo.DeclaringType.FullName}.{methodInfo.Name} " +
                $"has an invalid signature. {message}");
    }
}