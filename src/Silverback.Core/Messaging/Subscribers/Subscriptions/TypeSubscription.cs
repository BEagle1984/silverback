// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Util;

namespace Silverback.Messaging.Subscribers.Subscriptions
{
    /// <summary>
    ///     A subscription based on a type (can also be a base class or an interface).
    /// </summary>
    internal sealed class TypeSubscription : ISubscription
    {
        private readonly TypeSubscriptionOptions _options;

        private IReadOnlyList<SubscribedMethod>? _subscribedMethods;

        public TypeSubscription(Type subscribedType, TypeSubscriptionOptions options)
        {
            SubscribedType = Check.NotNull(subscribedType, nameof(subscribedType));
            _options = Check.NotNull(options, nameof(options));
        }

        public Type SubscribedType { get; }

        public IReadOnlyList<SubscribedMethod> GetSubscribedMethods(IServiceProvider serviceProvider) =>
            _subscribedMethods ??= serviceProvider
                .GetServices(SubscribedType)
                .SelectMany(subscriber => GetSubscribedMethods(subscriber, serviceProvider))
                .ToList();

        private SubscribedMethod GetSubscribedMethod(Type targetType, MethodInfo methodInfo)
        {
            var subscribeAttribute = methodInfo.GetCustomAttribute<SubscribeAttribute>();

            // TODO: Migrate to records
            var methodOptions = new TypeSubscriptionOptions
            {
                Filters = _options.Filters
                    .Union(methodInfo.GetCustomAttributes<MessageFilterAttribute>(false))
                    .Distinct().ToList(),
                Exclusive = subscribeAttribute?.Exclusive ?? _options.Exclusive,
                AutoSubscribeAllPublicMethods = _options.AutoSubscribeAllPublicMethods
            };

            return new SubscribedMethod(
                serviceProvider => serviceProvider.GetRequiredService(targetType),
                methodInfo,
                methodOptions);
        }

        private IEnumerable<SubscribedMethod> GetSubscribedMethods(
            object? subscriber,
            IServiceProvider serviceProvider)
        {
            if (subscriber == null)
                return Enumerable.Empty<SubscribedMethod>();

            var targetType = subscriber.GetType();

            return GetMethods(targetType)
                .Select(
                    methodInfo => GetSubscribedMethod(targetType, methodInfo)
                        .EnsureInitialized(serviceProvider))
                .ToList();
        }

        // Methods decorated with [Subscribe] can be declared in a base class.
        // If _autoSubscribeAllPublicMethods is set to true, we return all public methods,
        // discarding the ones with no parameters (that would cause an exception later on) and
        // the ones inherited from the base classes (to avoid calling object.Equals and similar methods).
        private IEnumerable<MethodInfo> GetMethods(Type type) =>
            type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(
                    methodInfo =>
                        methodInfo.GetCustomAttribute<SubscribeAttribute>(true) != null ||
                        _options.AutoSubscribeAllPublicMethods && methodInfo.IsPublic &&
                        !methodInfo.IsSpecialName &&
                        methodInfo.DeclaringType == type && methodInfo.GetParameters().Any());
    }
}
