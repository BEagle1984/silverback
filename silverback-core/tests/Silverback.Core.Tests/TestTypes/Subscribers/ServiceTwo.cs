using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.TestTypes.Subscribers
{
    public class ServiceTwo : IService
    {
        public int Handled { get; set; }

        [Subscribe]
        public void TestTwo(TestCommandTwo command)
        {
            Handled++;
        }

        [Subscribe]
        public async Task TestTwoAsync(TestCommandTwo command)
        {
            await Task.Run(async () =>
            {
                await Task.Delay(10);
                return Handled++;
            });
        }

        [Subscribe]
        public async Task OnCommit(TransactionCommitEvent message)
        {
            await Task.Run(async () =>
            {
                await Task.Delay(10);
                return Handled++;
            });
        }

        [Subscribe]
        public void OnRollback(TransactionRollbackEvent message)
        {
            Handled++;
        }
    }
}