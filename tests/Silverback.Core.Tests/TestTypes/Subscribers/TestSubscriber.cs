// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Subscribers;
using Silverback.Tests.Core.TestTypes.Messages;

namespace Silverback.Tests.Core.TestTypes.Subscribers
{
    public class TestSubscriber : ITestSubscriber, ISubscriber
    {
        public int ReceivedMessagesCount { get; private set; }

        public int ReceivedCallsCount { get; private set; }

        [Subscribe]
        public void OnTestMessageReceived(ITestMessage message)
        {
            ReceivedMessagesCount++;
            ReceivedCallsCount++;
        }

        [Subscribe]
        private void OnTestMessageReceived2(ITestMessage message)
        {
            ReceivedCallsCount++;
        }

        public void OnTestMessageReceived3(ITestMessage message)
        {
            ReceivedCallsCount++;
        }

        private void OnTestMessageReceived4(ITestMessage message)
        {
            ReceivedCallsCount++;
        }

        // This is here to try and break the reflection based logic -> DON'T REMOVE
        public void TryToBreakIt(int param)
        {
        }
    }
}
