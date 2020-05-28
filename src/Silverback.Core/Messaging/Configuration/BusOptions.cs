// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.Messaging.Subscribers.Subscriptions;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Stores the internal bus configuration (subscribers, etc.).
    /// </summary>
    public class BusOptions
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="BusOptions" /> class.
        /// </summary>
        public BusOptions()
        {
            Subscriptions.Add(new TypeSubscription(typeof(ISubscriber)));
            MessageTypes.Add(typeof(IMessage));
        }

        /// <summary>
        ///     Gets the collection of <see cref="ISubscription" />. A single subscription can resolve to multiple
        ///     subscribed methods.
        /// </summary>
        public IList<ISubscription> Subscriptions { get; } = new List<ISubscription>();

        /// <summary>
        ///     Gets the collection of handled message types. These types will be recognized as messages and thus
        ///     automatically republished when returned by a subscribed method.
        /// </summary>
        public IList<Type> MessageTypes { get; } = new List<Type>();
    }
}
