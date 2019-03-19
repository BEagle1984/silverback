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

        public TypeSubscription(Type subscriberType, bool autoSubscribeAllPublicMethods = true)
        {
            SubscriberType = subscriberType ?? throw new ArgumentNullException(nameof(subscriberType));
            _autoSubscribeAllPublicMethods = autoSubscribeAllPublicMethods;
        }

        public Type SubscriberType { get; }

        public IEnumerable<SubscribedMethod> GetSubscribedMethods(IServiceProvider serviceProvider) =>
            serviceProvider
                .GetServices(SubscriberType)
                .SelectMany(subscriber =>
                    GetMethods(subscriber.GetType())
                        .Select(methodInfo =>
                            new SubscribedMethod(subscriber, new SubscribedMethodInfo(methodInfo))))
                .ToList();

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