// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers.Subscriptions;

namespace Silverback.Messaging.Configuration
{
    internal sealed class BusOptions : IBusOptions
    {
        public BusOptions()
        {
            MessageTypes.Add(typeof(IMessage));
        }

        public IList<ISubscription> Subscriptions { get; } = new List<ISubscription>();

        public IList<Type> MessageTypes { get; } = new List<Type>();
    }
}
