using System;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;
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
