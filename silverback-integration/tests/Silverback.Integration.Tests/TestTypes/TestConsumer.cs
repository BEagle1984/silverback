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
        public bool IsReady { get; set; }

        public TestConsumer(IBroker broker, IEndpoint endpoint) 
            : base(broker, endpoint)
        {
        }

        public void TestPush(IIntegrationMessage message, IMessageSerializer serializer = null)
        {
            if (!Broker.IsConnected)
                throw new InvalidOperationException("The broker is not connected.");

            if (!IsReady)
                throw new InvalidOperationException("The consumer is not ready.");

            if (serializer == null)
                serializer = new JsonMessageSerializer();

            var envelope = Envelope.Create(message);
            var buffer = serializer.Serialize(envelope);

            HandleMessage(buffer);
        }
    }
}