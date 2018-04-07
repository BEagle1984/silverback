using System;
using Silverback.Messaging;
using Silverback.Messaging.Messages;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.TestTypes.Subscribers
{
    public class TestCustomSubscriber : Subscriber
    {
        public static int Counter { get; set; }

        public TestCustomSubscriber(IObservable<IMessage> messages) : base(messages)
        {
        }

        protected override void OnNext(IMessage message)
        {
            Counter++;
        }
    }
}
