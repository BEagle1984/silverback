// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Tests.Integration.TestTypes
{
    public class SilverbackEventsSubscriber : ISubscriber
    {
        public List<ISilverbackEvent> ReceivedEvents { get; } = new List<ISilverbackEvent>();

        public void OnMessageReceived(object message)
        {
            ReceivedEvents.Add((ISilverbackEvent) message);
        }
    }
}