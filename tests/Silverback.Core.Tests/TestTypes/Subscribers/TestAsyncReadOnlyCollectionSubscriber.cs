// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.Core.TestTypes.Messages;

namespace Silverback.Tests.Core.TestTypes.Subscribers
{
    public class TestAsyncReadOnlyCollectionSubscriber : ISubscriber
    {
        public int ReceivedMessagesCount { get; private set; }

        public int ReceivedBatchesCount { get; private set; }

        [Subscribe]
        public async Task OnTestMessagesReceivedAsync(IReadOnlyCollection<ITestMessage> messages)
        {
            await Task.Delay(1);
            ReceivedBatchesCount++;
            ReceivedMessagesCount += messages.Count;
        }
    }
}
