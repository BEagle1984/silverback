// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Mqtt.Mocks;

internal class SharedSubscriptionsManager
{
    private readonly Dictionary<string, GroupSubscriptions> _groupSubscriptions = new();

    public void Add(Subscription subscription)
    {
        if (subscription.SharedSubscriptionGroup == null)
            return;

        lock (_groupSubscriptions)
        {
            _groupSubscriptions.GetOrAdd(subscription.SharedSubscriptionGroup, _ => new GroupSubscriptions()).Add(subscription);
        }
    }

    public void Remove(Subscription subscription)
    {
        if (subscription.SharedSubscriptionGroup == null)
            return;

        lock (_groupSubscriptions)
        {
            if (_groupSubscriptions.TryGetValue(subscription.SharedSubscriptionGroup, out GroupSubscriptions? groupSubscriptions))
                groupSubscriptions.Remove(subscription);
        }
    }

    public bool IsActive(Subscription subscription)
    {
        if (subscription.SharedSubscriptionGroup == null)
            return true;

        lock (_groupSubscriptions)
        {
            if (_groupSubscriptions.TryGetValue(subscription.SharedSubscriptionGroup, out GroupSubscriptions? groupSubscriptions))
                return groupSubscriptions.ActiveSubscription == subscription;

            return true;
        }
    }

    private sealed class GroupSubscriptions
    {
        private readonly List<Subscription> _subscriptions = new();

        public Subscription? ActiveSubscription => _subscriptions.FirstOrDefault();

        public void Add(Subscription subscription) => _subscriptions.Add(subscription);

        public void Remove(Subscription subscription) => _subscriptions.Remove(subscription);
    }
}
