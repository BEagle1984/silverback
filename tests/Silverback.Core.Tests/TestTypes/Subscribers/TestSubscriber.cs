// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.Core.TestTypes.Messages;

namespace Silverback.Tests.Core.TestTypes.Subscribers;

public class TestSubscriber : ITestSubscriber
{
    public int ReceivedMessagesCount { get; private set; }

    public int ReceivedCallsCount { get; private set; }

    [Subscribe]
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = Justifications.CalledBySilverback)]
    [SuppressMessage("ReSharper", "UnusedParameter.Global", Justification = Justifications.CalledBySilverback)]
    [SuppressMessage("", "CA1801", Justification = Justifications.CalledBySilverback)]
    public void OnTestMessageReceived(ITestMessage message)
    {
        ReceivedMessagesCount++;
        ReceivedCallsCount++;
    }

    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = Justifications.CalledBySilverback)]
    [SuppressMessage("ReSharper", "UnusedParameter.Global", Justification = Justifications.CalledBySilverback)]
    [SuppressMessage("", "CA1801", Justification = Justifications.CalledBySilverback)]
    public void OnTestMessageReceived3(ITestMessage message)
    {
        ReceivedCallsCount++;
    }

    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = Justifications.CalledBySilverback)]
    [SuppressMessage("ReSharper", "UnusedParameter.Global", Justification = Justifications.CalledBySilverback)]
    [SuppressMessage("", "CA1822", Justification = Justifications.CalledBySilverback)]
    [SuppressMessage("", "CA1801", Justification = Justifications.CalledBySilverback)]
    public void TryToBreakIt(int param)
    {
        // This is here to try and break the reflection based subscribers discovery logic -> DON'T REMOVE
    }

    [Subscribe]
    [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = Justifications.CalledBySilverback)]
    [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = Justifications.CalledBySilverback)]
    [SuppressMessage("", "CA1801", Justification = Justifications.CalledBySilverback)]
    [SuppressMessage("", "IDE0051", Justification = Justifications.CalledBySilverback)]
    private async Task OnTestMessageReceived2(ITestMessage message)
    {
        await Task.Delay(1);
        ReceivedCallsCount++;
    }

    [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = Justifications.CalledBySilverback)]
    [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = Justifications.CalledBySilverback)]
    [SuppressMessage("", "CA1801", Justification = Justifications.CalledBySilverback)]
    [SuppressMessage("", "IDE0051", Justification = Justifications.CalledBySilverback)]
    private async Task OnTestMessageReceived4(ITestMessage message)
    {
        await Task.Delay(1);
        ReceivedCallsCount++;
    }
}
