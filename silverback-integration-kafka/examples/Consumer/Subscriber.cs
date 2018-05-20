using System;
using System.Collections.Generic;
using System.Text;
using Messages;
using Silverback.Messaging.Subscribers;

namespace Consumer
{
    public class Subscriber : Subscriber<TestMessage>
    {
        public override void Handle(TestMessage message)
        {
            Console.WriteLine($"Message '{message.Id}' consumed successfully!");
        }
    }
}
