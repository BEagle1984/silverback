// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;

namespace Silverback.Messaging.Subscribers.Subscriptions
{
    internal static class SubscriptionCollectionExtensions
    {
        public static void AddTypeSubscriptionIfNotExists(
            this ICollection<ISubscription> collection,
            Type subscriberType,
            TypeSubscriptionOptions options)
        {
            bool exists = collection.OfType<TypeSubscription>()
                .Any(existingSubscription => existingSubscription.SubscribedType == subscriberType);

            if (!exists)
                collection.Add(new TypeSubscription(subscriberType, options));
        }
    }
}
