// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
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