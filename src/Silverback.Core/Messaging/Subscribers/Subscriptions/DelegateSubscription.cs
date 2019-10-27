// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Subscribers.Subscriptions
{
    public class DelegateSubscription : ISubscription
    {
        private readonly SubscribedMethod _method;

        public DelegateSubscription(Delegate handler, SubscriptionOptions options)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            _method = new SubscribedMethod(
                _ => handler.Target, 
                handler.Method, 
                options?.Exclusive, 
                options?.Parallel, 
                options?.MaxDegreeOfParallelism);
        }

        public IEnumerable<SubscribedMethod> GetSubscribedMethods(IServiceProvider _) =>
            new[] { _method };
    }
}