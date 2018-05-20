using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.TestTypes
{
    public class TestProducer : Producer
    {
        public List<byte[]> SentMessages { get; }

        public TestProducer(TestBroker broker, IEndpoint endpoint)
            : base(broker, endpoint)
        {
            SentMessages = broker.SentMessages;
        }

        protected override void Produce(IIntegrationMessage message, byte[] serializedMessage)
        {
            SentMessages.Add(serializedMessage);
        }

        protected override Task ProduceAsync(IIntegrationMessage message, byte[] serializedMessage)
        {
            Produce(message, serializedMessage);
            return Task.CompletedTask;
        }
    }
}