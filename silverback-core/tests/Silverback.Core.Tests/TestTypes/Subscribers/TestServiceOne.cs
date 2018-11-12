using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.TestTypes.Messages;

namespace Silverback.Tests.TestTypes.Subscribers
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