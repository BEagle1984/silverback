// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.Core.TestTypes.Messages;

namespace Silverback.Tests.Core.TestTypes.Subscribers;

public class TestRepublisher
{
    public int ReceivedMessagesCount { get; private set; }

    [Subscribe]
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = Justifications.CalledBySilverback)]
    [SuppressMessage("ReSharper", "UnusedParameter.Global", Justification = Justifications.CalledBySilverback)]
    [SuppressMessage("", "CA1801", Justification = Justifications.CalledBySilverback)]
    public TestEventOne OnRequestReceived(TestCommandOne message)
    {
        ReceivedMessagesCount++;

        return new TestEventOne();
    }

    [Subscribe]
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = Justifications.CalledBySilverback)]
    [SuppressMessage("ReSharper", "UnusedParameter.Global", Justification = Justifications.CalledBySilverback)]
    [SuppressMessage("", "CA1801", Justification = Justifications.CalledBySilverback)]
    public IMessage[] OnRequestReceived(TestCommandTwo message)
    {
        ReceivedMessagesCount++;

        return new IMessage[] { new TestEventOne(), new TestEventTwo() };
    }
}
