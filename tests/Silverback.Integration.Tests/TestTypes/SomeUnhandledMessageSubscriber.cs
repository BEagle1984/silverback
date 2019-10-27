// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Subscribers;

namespace Silverback.Tests.Integration.TestTypes
{
    public class SomeUnhandledMessageSubscriber : ISubscriber
    {
        public List<SomeUnhandledMessage> ReceivedMessages { get; } = new List<SomeUnhandledMessage>();

        [Subscribe]
        void OnMessageReceived(SomeUnhandledMessage message) => ReceivedMessages.Add(message);
    }
}