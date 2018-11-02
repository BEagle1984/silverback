using System.Threading.Tasks;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.TestTypes.Subscribers
{
    public class ServiceOne : IService
    {
        public int Handled { get; set; }

        [Subscribe]
        public void TestOne(TestCommandOne command)
        {
            Handled++;
        }

        [Subscribe]
        public async Task TestOneAsync(TestCommandOne command)
        {
            await Task.Run(async () =>
            {
                await Task.Delay(10);
                return Handled++;
            });
        }
    }
}