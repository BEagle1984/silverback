using System;
using System.Collections.Generic;
using System.Text;
using Silverback.Messaging;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Core.Tests.TestTypes.Subscribers
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
