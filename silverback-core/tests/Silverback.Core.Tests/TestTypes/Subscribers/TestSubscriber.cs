using System;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.TestTypes.Messages;

namespace Silverback.Tests.TestTypes.Subscribers
{
    public class TestSubscriber : Subscriber<ITestMessage>
    {
        public TestSubscriber(ILogger<TestSubscriber> logger) : base(logger)
        {
        }

        public int ReceivedMessagesCount { get; private set; }

        public override void Handle(ITestMessage message) => ReceivedMessagesCount++;
    }
}
