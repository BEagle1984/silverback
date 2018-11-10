using System;
using System.Threading.Tasks;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.TestTypes.Subscribers
{
    public class TestCommandOneAsyncSubscriber : AsyncSubscriber<TestCommandOne>
    {
        public int Handled { get; set; }

        public override async Task HandleAsync(TestCommandOne message)
        {
            await Task.Delay(1);
            Handled++;
        }
    }
}