﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Subscribers.Subscriptions
{
    /// <summary>
    ///     Represents a subscription initialized with a method delegate.
    /// </summary>
    internal class DelegateSubscription : ISubscription
    {
        private readonly SubscribedMethod _method;

        public DelegateSubscription(Delegate handler, SubscriptionOptions? options)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            _method = new SubscribedMethod(
                _ => handler.Target,
                handler.Method,
                options?.Exclusive,
                options?.Parallel,
                options?.MaxDegreeOfParallelism);
        }

        public IReadOnlyCollection<SubscribedMethod> GetSubscribedMethods(IServiceProvider serviceProvider) =>
            new[] { _method };
    }
}