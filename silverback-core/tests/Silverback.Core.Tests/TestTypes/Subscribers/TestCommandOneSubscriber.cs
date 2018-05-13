using Silverback.Messaging.Subscribers;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.TestTypes.Subscribers
{
    public class TestCommandOneSubscriber : Subscriber<TestCommandOne>
    {
        public int Counter { get; set; }

        public override void Handle(TestCommandOne message)
        {
            Counter++;
        }
    }
}