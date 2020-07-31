// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.Core.TestTypes.Messages;

namespace Silverback.Tests.Core.TestTypes.Subscribers
{
    public class TestAsyncEnumerableSubscriber
    {
        public int ReceivedMessagesCount { get; private set; }

        public int ReceivedBatchesCount { get; private set; }

        [Subscribe]
        public async Task OnTestMessagesReceivedAsync(IEnumerable<ITestMessage> messages)
        {
            await Task.Delay(1);
            ReceivedBatchesCount++;
            ReceivedMessagesCount += messages.Count();
        }
    }
}
