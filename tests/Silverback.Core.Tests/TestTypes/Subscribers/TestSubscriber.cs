// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.Core.TestTypes.Messages;

namespace Silverback.Tests.Core.TestTypes.Subscribers
{
    public class TestSubscriber : ITestSubscriber, ISubscriber
    {
        public int ReceivedMessagesCount { get; private set; }

        public int ReceivedCallsCount { get; private set; }

        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("ReSharper", "UnusedParameter.Global", Justification = Justifications.CalledBySilverback)]
        [Subscribe]
        public void OnTestMessageReceived(ITestMessage message)
        {
            ReceivedMessagesCount++;
            ReceivedCallsCount++;
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("ReSharper", "UnusedParameter.Global", Justification = Justifications.CalledBySilverback)]
        public void OnTestMessageReceived3(ITestMessage message)
        {
            ReceivedCallsCount++;
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("ReSharper", "UnusedParameter.Global", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("", "CA1822", Justification = Justifications.CalledBySilverback)]
        public void TryToBreakIt(int param)
        {
            // This is here to try and break the reflection based subscribers discovery logic -> DON'T REMOVE
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = Justifications.CalledBySilverback)]
        [Subscribe]
        private void OnTestMessageReceived2(ITestMessage message)
        {
            ReceivedCallsCount++;
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = Justifications.CalledBySilverback)]
        private void OnTestMessageReceived4(ITestMessage message)
        {
            ReceivedCallsCount++;
        }
    }
}
