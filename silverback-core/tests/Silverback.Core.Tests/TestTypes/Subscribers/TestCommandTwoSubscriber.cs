using System;
using Silverback.Messaging;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.TestTypes.Subscribers
{
    public class TestCommandTwoSubscriber : Subscriber<TestCommandTwo>
    {
        public static int Counter { get; set; }

        public TestCommandTwoSubscriber(IObservable<TestCommandTwo> messages) : base(messages)
        {
        }

        protected override void OnNext(TestCommandTwo message)
        {
            Counter++;
        }
    }
}