using System;
using System.Threading.Tasks;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.TestTypes.Subscribers
{
    public class TestCommandTwoAsyncSubscriber : AsyncSubscriber<TestCommandTwo>
    {
        public int Handled { get; set; }

        public override async Task HandleAsync(TestCommandTwo message)
        {
            await Task.Delay(1);
            Handled++;
        }
    }
}