// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using Silverback.Core.Tests.TestTypes.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Core.Tests.TestTypes.Subscribers
{
    public class TestEnumerableSubscriber : ISubscriber
    {
        public int ReceivedMessagesCount { get; private set; }
        public int ReceivedBatchesCount { get; private set; }

        [Subscribe]
        public void OnTestMessagesReceived(IEnumerable<ITestMessage> messages)
        {
            ReceivedBatchesCount++;
            ReceivedMessagesCount += messages.Count();
        }
    }
}