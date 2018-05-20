using System;
using System.Collections.Generic;
using System.Text;
using Messages;
using Silverback.Messaging.Subscribers;

namespace Producer
{
    public class Subscriber : Subscriber<TestMessage>
    {
        public override void Handle(TestMessage message)
        {
            Console.WriteLine($"Message '{message.Id}' delivered successfully!");
        }
    }
}
