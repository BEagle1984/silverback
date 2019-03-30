// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.Core.TestTypes.Messages;

namespace Silverback.Tests.Core.TestTypes.Subscribers
{
    public class TestRepublisher : ISubscriber
    {
        public int ReceivedMessagesCount { get; private set; }

        [Subscribe]
        public TestEventOne OnRequestReceived(TestCommandOne message)
        {
            ReceivedMessagesCount++;

            return new TestEventOne();
        }

        [Subscribe]
        public IMessage[] OnRequestReceived(TestCommandTwo message)
        {
            ReceivedMessagesCount++;

            return new IMessage[] { new TestEventOne(), new TestEventTwo() };
        }
    }
}