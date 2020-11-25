// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;

namespace Silverback.Messaging.Subscribers.Subscriptions
{
    internal static class SubscriptionCollectionExtensions
    {
        public static void AddTypedSubscriptionIfNotExists(
            this ICollection<ISubscription> collection,
            Type subscriberType,
            bool autoSubscribeAllPublicMethods)
        {
            bool exists = collection.OfType<TypeSubscription>()
                .Any(existingSubscription => existingSubscription.SubscribedType == subscriberType);

            if (exists)
                return;

            collection.Add(new TypeSubscription(subscriberType, autoSubscribeAllPublicMethods));
        }
    }
}
