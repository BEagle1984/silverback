// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

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
        public void TestOne(TestCommandOne command) => ReceivedMessagesCount++;

        [Subscribe]
        public async Task TestOneAsync(TestCommandOne command) =>
            await Task.Run(async () =>
            {
                await Task.Delay(10);
                return ReceivedMessagesCount++;
            });

        [Subscribe]
        public void OnCommit(TransactionAbortedEvent message) => ReceivedMessagesCount++;

        [Subscribe]
        public async Task OnRollback(TransactionAbortedEvent message) =>
            await Task.Run(async () =>
            {
                await Task.Delay(10);
                return ReceivedMessagesCount++;
            });
    }
}