using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Tests.TestTypes.Subscribers
{
    public class TestService : IService
    {

        public int Handled { get; set; }

        [Subscribe]
        public void OnCommit(TransactionCommitEvent message)
        {
            Handled++;
        }

        [Subscribe]
        public async Task OnRollback(TransactionRollbackEvent message)
        {
            await Task.Run(async () =>
            {
                await Task.Delay(10);
                return Handled++;
            });
        }
    }
}