 using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.TestTypes.Messages;

namespace Silverback.Tests.TestTypes.Subscribers
{
    public class TestAsyncSubscriber : ISubscriber
    {
        public int ReceivedMessagesCount { get; private set; }

        [Subscribe]
        public async Task OnTestMessageReceivedAsync(ITestMessage message)
        {
            await Task.Delay(1);
            ReceivedMessagesCount++;
        }
    }
}