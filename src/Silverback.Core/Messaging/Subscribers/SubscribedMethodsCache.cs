// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Util;

namespace Silverback.Messaging.Subscribers;

internal sealed class SubscribedMethodsCache
{
    private readonly SubscribedMethodsCacheSingleton _cacheSingleton;

    private readonly IServiceProvider _serviceProvider;

    public SubscribedMethodsCache(SubscribedMethodsCacheSingleton cacheSingleton, IServiceProvider serviceProvider)
    {
        _cacheSingleton = Check.NotNull(cacheSingleton, nameof(cacheSingleton));
        _serviceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
    }

    public bool HasAnyMessageStreamSubscriber => _cacheSingleton.HasAnyMessageStreamSubscriber(_serviceProvider);

    public IReadOnlyList<SubscribedMethod> GetExclusiveMethods(object message) =>
        _cacheSingleton.GetExclusiveMethods(message, _serviceProvider);

    public IReadOnlyList<SubscribedMethod> GetNonExclusiveMethods(object message) =>
        _cacheSingleton.GetNonExclusiveMethods(message, _serviceProvider);
}
