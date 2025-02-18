// Copyright (c) 2024 Sergio Aquilini
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
    private readonly object? _implementationInstance;

    public TypeSubscription(Type subscriberType, TypeSubscriptionOptions options)
    {
        SubscriberType = Check.NotNull(subscriberType, nameof(subscriberType));
        Options = Check.NotNull(options, nameof(options));
    }

    public TypeSubscription(Type subscriberType, object implementationInstance, TypeSubscriptionOptions options)
        : this(subscriberType, options)
    {
        _implementationInstance = Check.NotNull(implementationInstance, nameof(implementationInstance));
    }

    public Type SubscriberType { get; }

    public TypeSubscriptionOptions Options { get; }

    public IReadOnlyList<SubscribedMethod> GetSubscribedMethods(IServiceProvider serviceProvider) =>
        GetMethods(SubscriberType)
            .Select(methodInfo => GetSubscribedMethod(SubscriberType, methodInfo).EnsureInitialized(serviceProvider))
            .ToArray();

    private SubscribedMethod GetSubscribedMethod(Type targetType, MethodInfo methodInfo)
    {
        SubscribeAttribute? subscribeAttribute = methodInfo.GetCustomAttribute<SubscribeAttribute>();

        TypeSubscriptionOptions methodOptions = new()
        {
            Filters = Options.Filters
                .Union(methodInfo.GetCustomAttributes<MessageFilterAttribute>(false))
                .Distinct().ToList(),
            IsExclusive = subscribeAttribute?.Exclusive ?? Options.IsExclusive,
            AutoSubscribeAllPublicMethods = Options.AutoSubscribeAllPublicMethods
        };

        return new SubscribedMethod(
            _implementationInstance == null
                ? serviceProvider => serviceProvider.GetRequiredService(targetType)
                : _ => _implementationInstance,
            methodInfo,
            methodOptions);
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
                    methodInfo.DeclaringType == type && methodInfo.GetParameters().Length != 0);
}
