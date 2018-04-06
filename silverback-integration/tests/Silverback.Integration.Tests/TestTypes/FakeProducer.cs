using System.Collections.Generic;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.TestTypes
{
    public class FakeProducer : Producer
    {
        public List<byte[]> SentMessages { get; }

        public FakeProducer(IEndpoint endpoint)
            : base(endpoint)
        {
            SentMessages = endpoint.GetBroker<FakeBroker>().SentMessages;
        }

        protected override void Produce(IIntegrationMessage message, byte[] serializedMessage)
        {
            SentMessages.Add(serializedMessage);
        }
    }
}