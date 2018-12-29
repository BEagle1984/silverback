// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Core.Tests.TestTypes.Messages;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Core.Tests.TestTypes.Subscribers
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
        public void OnCommit(TransactionCommitEvent message) => ReceivedMessagesCount++;

        [Subscribe]
        public async Task OnRollback(TransactionRollbackEvent message) =>
            await Task.Run(async () =>
            {
                await Task.Delay(10);
                return ReceivedMessagesCount++;
            });
    }
}