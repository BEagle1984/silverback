// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Subscribers;
using Silverback.Tests.TestTypes.Messages;

namespace Silverback.Tests.TestTypes.Subscribers
{
    public class TestSubscriber : ISubscriber
    {
        public int ReceivedMessagesCount { get; private set; }

        [Subscribe]
        public void OnTestMessageReceived(ITestMessage message) => ReceivedMessagesCount++;
    }
}
