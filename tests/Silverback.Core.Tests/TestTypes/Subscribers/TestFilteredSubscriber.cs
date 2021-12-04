// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Tests.Core.TestTypes.Messages;

namespace Silverback.Tests.Core.TestTypes.Subscribers;

public class TestFilteredSubscriber
{
    public int ReceivedMessagesCount { get; private set; }

    public int ReceivedEnvelopesCount { get; private set; }

    [TestMessageFilter]
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = Justifications.CalledBySilverback)]
    [SuppressMessage("ReSharper", "UnusedParameter.Global", Justification = Justifications.CalledBySilverback)]
    [SuppressMessage("", "CA1801", Justification = Justifications.CalledBySilverback)]
    public void OnMessageReceived(ITestMessage message) => ReceivedMessagesCount++;

    [TestMessageFilter]
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = Justifications.CalledBySilverback)]
    [SuppressMessage("ReSharper", "UnusedParameter.Global", Justification = Justifications.CalledBySilverback)]
    [SuppressMessage("", "CA1801", Justification = Justifications.CalledBySilverback)]
    public Task OnEnvelopeReceived(IEnvelope message)
    {
        ReceivedEnvelopesCount++;
        return Task.CompletedTask;
    }
}
