using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Messages;
using Silverback.Messaging.Subscribers;

namespace AsyncProducer
{
    public class SubscriberAsync : AsyncSubscriber<TestMessage>
    {
        public override async Task HandleAsync(TestMessage message)
        {
            await Task.Delay(2000);
            Console.WriteLine($"Message '{message.Id}' delivered successfully!");
        }
    }
}
