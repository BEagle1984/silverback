using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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


    public class SubscriberAsync : AsyncSubscriber<TestMessage>
    {
        public override async Task HandleAsync(TestMessage message)
        {
            await Task.Delay(2000);
            Console.WriteLine($"Message '{message.Id}' delivered successfully!");
        }
    }
}
