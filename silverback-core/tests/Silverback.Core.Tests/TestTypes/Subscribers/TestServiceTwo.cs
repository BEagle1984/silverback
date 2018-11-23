using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.TestTypes.Domain;
using Silverback.Tests.TestTypes.Messages;

namespace Silverback.Tests.TestTypes.Subscribers
{
    public class TestServiceTwo : IService
    {
        public int ReceivedMessagesCount { get; set; }

        [Subscribe]
        public void TestTwo(TestCommandTwo command) => ReceivedMessagesCount++;

        [Subscribe]
        public async Task TestTwoAsync(TestCommandTwo command) =>
            await Task.Run(async () =>
            {
                await Task.Delay(10);
                return ReceivedMessagesCount++;
            });

        [Subscribe]
        public async Task OnCommit(TransactionCommitEvent message) =>
            await Task.Run(async () =>
            {
                await Task.Delay(10);
                return ReceivedMessagesCount++;
            });

        [Subscribe]
        public void OnRollback(TransactionRollbackEvent message) => ReceivedMessagesCount++;
    }
}