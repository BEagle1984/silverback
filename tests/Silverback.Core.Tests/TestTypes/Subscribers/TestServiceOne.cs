// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.Core.TestTypes.Messages;

namespace Silverback.Tests.Core.TestTypes.Subscribers
{
    public class TestServiceOne : IService
    {
        public int ReceivedMessagesCount { get; set; }

        [Subscribe]
        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("ReSharper", "UnusedParameter.Global", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("", "CA1801", Justification = Justifications.CalledBySilverback)]
        public void TestOne(TestCommandOne command) => ReceivedMessagesCount++;

        [Subscribe]
        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("ReSharper", "UnusedParameter.Global", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("", "CA1801", Justification = Justifications.CalledBySilverback)]
        public async Task TestOneAsync(TestCommandOne command) =>
            await Task.Run(async () =>
            {
                await Task.Delay(10);
                return ReceivedMessagesCount++;
            });

        [Subscribe]
        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("ReSharper", "UnusedParameter.Global", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("", "CA1801", Justification = Justifications.CalledBySilverback)]
        public void OnCommit(TransactionAbortedEvent message) => ReceivedMessagesCount++;

        [Subscribe]
        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("ReSharper", "UnusedParameter.Global", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("", "CA1801", Justification = Justifications.CalledBySilverback)]
        public async Task OnRollback(TransactionAbortedEvent message) =>
            await Task.Run(async () =>
            {
                await Task.Delay(10);
                return ReceivedMessagesCount++;
            });
    }
}