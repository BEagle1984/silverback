// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.Core.TestTypes.Messages;

namespace Silverback.Tests.Core.TestTypes.Subscribers
{
    public class TestAsyncSubscriber
    {
        public int ReceivedMessagesCount { get; private set; }

        [Subscribe]
        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("ReSharper", "UnusedParameter.Global", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("", "CA1801", Justification = Justifications.CalledBySilverback)]
        public async Task OnTestMessageReceivedAsync(ITestMessage message)
        {
            await Task.Delay(1);
            ReceivedMessagesCount++;
        }
    }
}
