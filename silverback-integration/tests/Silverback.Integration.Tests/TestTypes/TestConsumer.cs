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
        private Action<byte[]> _handler;
        private readonly List<Action<byte[]>> _consumers;

        public TestConsumer(IBroker broker, IEndpoint endpoint, List<Action<byte[]>> consumers) 
            : base(broker, endpoint)
        {
            _consumers = consumers;
        }

        public void TestPush(IIntegrationMessage message, IMessageSerializer serializer = null)
        {
            if (serializer == null)
                serializer = new JsonMessageSerializer();

            var envelope = Envelope.Create(message);
            var serialized = serializer.Serialize(envelope);
            _consumers.ForEach(c => c.Invoke(serialized));
        }

        protected override void StartConsuming(Action<byte[]> handler)
        {
            _handler = handler;
            _consumers.Add(_handler);
        }

        protected override void StopConsuming()
        {
            _handler = null;
            _consumers.Remove(_handler);
        }
    }
}