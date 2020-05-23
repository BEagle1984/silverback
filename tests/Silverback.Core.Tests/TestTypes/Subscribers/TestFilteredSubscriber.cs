// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.Core.TestTypes.Messages;

namespace Silverback.Tests.Core.TestTypes.Subscribers
{
    public class TestFilteredSubscriber : ISubscriber
    {
        public int ReceivedMessagesCount { get; private set; }

        public int ReceivedEnvelopesCount { get; private set; }

        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("ReSharper", "UnusedParameter.Global", Justification = Justifications.CalledBySilverback)]
        [TestMessageFilter]
        public void OnMessageReceived(ITestMessage message) => ReceivedMessagesCount++;

        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("ReSharper", "UnusedParameter.Global", Justification = Justifications.CalledBySilverback)]
        [TestMessageFilter]
        public Task OnEnvelopeReceived(IEnvelope message)
        {
            ReceivedEnvelopesCount++;
            return Task.CompletedTask;
        }
    }
}