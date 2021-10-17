// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Util;

namespace Silverback.Messaging.Subscribers.Subscriptions
{
    /// <summary>
    ///     Represents a subscription initialized with a method delegate.
    /// </summary>
    internal sealed class DelegateSubscription : ISubscription
    {
        private readonly SubscribedMethod _method;

        public DelegateSubscription(Delegate handler, SubscriptionOptions? options)
        {
            Check.NotNull(handler, nameof(handler));

            _method = new SubscribedMethod(_ => handler.Target, handler.Method, options?.Exclusive);
        }

        public IReadOnlyCollection<SubscribedMethod> GetSubscribedMethods(IServiceProvider serviceProvider)
        {
            return new[] { _method.EnsureInitialized(serviceProvider) };
        }
    }
}
