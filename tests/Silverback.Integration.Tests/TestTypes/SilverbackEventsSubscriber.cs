// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.TestTypes;

public class SilverbackEventsSubscriber
{
    public IList<ISilverbackEvent> ReceivedEvents { get; } = new List<ISilverbackEvent>();

    public void OnMessageReceived(ISilverbackEvent message)
    {
        ReceivedEvents.Add(message);
    }
}
