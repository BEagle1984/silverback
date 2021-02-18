// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Util;

namespace Silverback.Messaging.Subscribers
{
    internal class SubscribedMethodsCache : ISubscribedMethodsCache
    {
        private readonly SubscribedMethodsCacheSingleton _cacheSingleton;

        private readonly IServiceProvider _serviceProvider;

        public SubscribedMethodsCache(
            SubscribedMethodsCacheSingleton cacheSingleton,
            IServiceProvider serviceProvider)
        {
            _cacheSingleton = Check.NotNull(cacheSingleton, nameof(cacheSingleton));
            _serviceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
        }

        // TODO: check by type
        public bool HasAnyMessageStreamSubscriber =>
            _cacheSingleton.HasAnyMessageStreamSubscriber(_serviceProvider);

        public bool IsSubscribed(object message) => _cacheSingleton.IsSubscribed(message, _serviceProvider);

        public IEnumerable<SubscribedMethod> GetExclusiveMethods(object message) =>
            _cacheSingleton.GetExclusiveMethods(message, _serviceProvider);

        public IEnumerable<SubscribedMethod> GetNonExclusiveMethods(object message) =>
            _cacheSingleton.GetNonExclusiveMethods(message, _serviceProvider);
    }
}
