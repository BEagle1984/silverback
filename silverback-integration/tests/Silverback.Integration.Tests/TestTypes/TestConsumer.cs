using System;
using System.Collections.Generic;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Tests.TestTypes
{
    public class TestConsumer : Consumer
    {
        private readonly List<System.Action<byte[]>> _consumers;
        public TestConsumer(IEndpoint endpoint, List<System.Action<byte[]>> consumers) 
            : base(endpoint)
        {
            _consumers = consumers;
        }

        public void TestPush(IIntegrationMessage message, IMessageSerializer serializer = null)
        {
            if (serializer == null)
                serializer = new JsonMessageSerializer();

            var envelope = Envelope.Create(message);
            var serialized = serializer.Serialize(envelope);
            _consumers.ForEach(c => c(serialized));
        }

        protected override void Consume(Action<byte[]> handler)
        {
             _consumers.Add(handler);
        }
    }
}