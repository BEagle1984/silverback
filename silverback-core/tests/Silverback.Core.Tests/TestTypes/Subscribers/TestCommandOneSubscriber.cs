using System;
using Silverback.Messaging;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.TestTypes.Subscribers
{
    public class TestCommandOneSubscriber : Subscriber<TestCommandOne>
    {
        public static int Counter { get; set; }

        public TestCommandOneSubscriber(IObservable<TestCommandOne> messages) : base(messages)
        {
        }

        protected override void OnNext(TestCommandOne message)
        {
            Counter++;
        }
    }
}
