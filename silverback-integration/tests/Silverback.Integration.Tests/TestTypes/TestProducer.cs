using System.Collections.Generic;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.TestTypes
{
    public class TestProducer : Producer
    {
        public List<byte[]> SentMessages { get; }

        public TestProducer(IEndpoint endpoint)
            : base(endpoint)
        {
            SentMessages = endpoint.GetBroker<TestBroker>().SentMessages;
        }

        protected override void Produce(IIntegrationMessage message, byte[] serializedMessage)
        {
            SentMessages.Add(serializedMessage);
        }
    }
}