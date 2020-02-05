// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.Core.TestTypes.Messages;

#pragma warning disable 1998

namespace Silverback.Tests.Core.TestTypes.Subscribers
{
    public class TestFilteredSubscriber : ISubscriber
    {
        public int ReceivedMessagesCount { get; private set; }
        
        public int ReceivedEnvelopesCount { get; private set; }

        [TestMessageFilter]
        public void OnMessageReceived(ITestMessage _) => ReceivedMessagesCount++;
        
        [TestMessageFilter]
        public async Task OnEnvelopeReceived(IEnvelope _) => ReceivedEnvelopesCount++;
    }
}