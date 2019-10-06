// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Silverback.Messaging.Subscribers.Subscriptions
{
    public class TypeSubscription : ISubscription
    {
        private readonly bool _autoSubscribeAllPublicMethods;
        private IEnumerable<SubscribedMethod> _subscribedMethods;

        public TypeSubscription(Type subscribedType, bool autoSubscribeAllPublicMethods = true)
        {
            SubscribedType = subscribedType ?? throw new ArgumentNullException(nameof(subscribedType));
            _autoSubscribeAllPublicMethods = autoSubscribeAllPublicMethods;
        }

        public Type SubscribedType { get; }

        public IEnumerable<SubscribedMethod> GetSubscribedMethods(IServiceProvider serviceProvider) =>
            _subscribedMethods ?? (_subscribedMethods =
                serviceProvider
                    .GetServices(SubscribedType)
                    .SelectMany(GetSubscribedMethods)
                    .ToList());

        private IEnumerable<SubscribedMethod> GetSubscribedMethods(object subscriber)
        {
            var targetType = subscriber.GetType();

            return GetMethods(targetType)
                .Select(methodInfo => GetSubscribedMethod(targetType, methodInfo))
                .ToList();
        }

        private SubscribedMethod GetSubscribedMethod(Type targetType, MethodInfo methodInfo)
        {
            var subscribeAttribute = methodInfo.GetCustomAttribute<SubscribeAttribute>();

            return new SubscribedMethod(
                serviceProvider => serviceProvider.GetRequiredService(targetType),
                methodInfo,
                subscribeAttribute?.Exclusive,
                subscribeAttribute?.Parallel,
                subscribeAttribute?.GetMaxDegreeOfParallelism());
        }

        // Methods decorated with [Subscribe] can be declared in a base class.
        // If _autoSubscribeAllPublicMethods is set to true, we return all public methods,
        // discarding the ones with no parameters (that would cause an exception later on) and
        // the ones inherited from the base classes (to avoid calling object.Equals and similar methods).
        private IEnumerable<MethodInfo> GetMethods(Type type) =>
            type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.GetCustomAttribute<SubscribeAttribute>(true) != null ||
                            _autoSubscribeAllPublicMethods && m.IsPublic &&
                            m.DeclaringType == type && m.GetParameters().Any());
    }
}