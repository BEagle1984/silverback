// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Subscribers.Subscriptions
{
    public interface ISubscription
    {
        IEnumerable<SubscribedMethod> GetSubscribedMethods(IServiceProvider serviceProvider);
    }
}