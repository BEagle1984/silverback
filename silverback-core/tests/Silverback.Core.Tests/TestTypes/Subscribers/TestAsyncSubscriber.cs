 using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
 using Silverback.Tests.TestTypes.Messages;

namespace Silverback.Tests.TestTypes.Subscribers
{
    public class TestAsyncSubscriber : AsyncSubscriber<ITestMessage>
    {
        public TestAsyncSubscriber(ILogger<TestAsyncSubscriber> logger) : base(logger)
        {
        }

        public int ReceivedMessagesCount { get; private set; }

        public override async Task HandleAsync(ITestMessage message)
        {
            await Task.Delay(1);
            ReceivedMessagesCount++;
        }
    }
}