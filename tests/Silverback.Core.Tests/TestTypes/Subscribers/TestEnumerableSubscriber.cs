// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.Core.TestTypes.Messages;

namespace Silverback.Tests.Core.TestTypes.Subscribers
{
    public class TestEnumerableSubscriber
    {
        public int ReceivedMessagesCount { get; private set; }

        public int ReceivedBatchesCount { get; private set; }

        [Subscribe]
        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = Justifications.CalledBySilverback)]
        public void OnTestMessagesReceived(IEnumerable<ITestMessage> messages)
        {
            ReceivedBatchesCount++;
            ReceivedMessagesCount += messages.Count();
        }
    }
}
