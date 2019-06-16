// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Subscribers
{
    public class SubscribedMethod
    {
        public SubscribedMethod(object target, SubscribedMethodInfo info)
        {
            Target = target;
            Info = info;
        }

        public object Target { get; }

        public SubscribedMethodInfo Info { get; }

        // TODO: Add (and use) added argument resolvers
    }
}