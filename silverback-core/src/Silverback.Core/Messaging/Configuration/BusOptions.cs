// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.Messaging.Subscribers.Subscriptions;

namespace Silverback.Messaging.Configuration
{
    public class BusOptions
    {
        public BusOptions()
        {
            Subscriptions.Add(new TypeSubscription(typeof(ISubscriber)));
            MessageTypes.Add(typeof(IMessage));
        }

        public List<ISubscription> Subscriptions { get; } = new List<ISubscription>();

        public List<Type> MessageTypes { get; } = new List<Type>();
    }
}