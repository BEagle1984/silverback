// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Util;

namespace Silverback.Messaging.Subscribers.Subscriptions;

/// <summary>
///     A subscription based on a type (can also be a base class or an interface).
/// </summary>
internal sealed class TypeSubscription : ISubscription
{
    private IReadOnlyList<SubscribedMethod>? _subscribedMethods;

    public TypeSubscription(Type subscribedType, TypeSubscriptionOptions options)
    {
        SubscribedType = Check.NotNull(subscribedType, nameof(subscribedType));
        Options = Check.NotNull(options, nameof(options));
    }

    public Type SubscribedType { get; }

    public TypeSubscriptionOptions Options { get; }

    public IReadOnlyList<SubscribedMethod> GetSubscribedMethods(IServiceProvider serviceProvider) =>
        _subscribedMethods ??= serviceProvider
            .GetServices(SubscribedType)
            .SelectMany(subscriber => GetSubscribedMethods(subscriber, serviceProvider))
            .ToList();

    private SubscribedMethod GetSubscribedMethod(Type targetType, MethodInfo methodInfo)
    {
        SubscribeAttribute? subscribeAttribute = methodInfo.GetCustomAttribute<SubscribeAttribute>();

        TypeSubscriptionOptions methodOptions = new()
        {
            Filters = Options.Filters
                .Union(methodInfo.GetCustomAttributes<MessageFilterAttribute>(false))
                .Distinct().ToList(),
            Exclusive = subscribeAttribute?.Exclusive ?? Options.Exclusive,
            AutoSubscribeAllPublicMethods = Options.AutoSubscribeAllPublicMethods
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

        Type targetType = subscriber.GetType();

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
                    Options.AutoSubscribeAllPublicMethods && methodInfo.IsPublic &&
                    !methodInfo.IsSpecialName &&
                    methodInfo.DeclaringType == type && methodInfo.GetParameters().Any());
}
