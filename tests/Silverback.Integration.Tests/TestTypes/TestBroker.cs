// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.TestTypes
{
    public class TestBroker : Broker
    {
        public TestBroker(IEnumerable<IBrokerBehavior> behaviors = null)
            : base(behaviors, NullLoggerFactory.Instance)
        {
        }

        public List<TestConsumer> Consumers { get; } = new List<TestConsumer>();

        public List<ProducedMessage> ProducedMessages { get; } = new List<ProducedMessage>();

        protected override IProducer InstantiateProducer(
            IProducerEndpoint endpoint,
            IEnumerable<IProducerBehavior> behaviors) =>
            new TestProducer(this, (TestProducerEndpoint) endpoint, behaviors);

        protected override IConsumer InstantiateConsumer(
            IConsumerEndpoint endpoint,
            IEnumerable<IConsumerBehavior> behaviors)
        {
            var consumer = new TestConsumer(this, (TestConsumerEndpoint) endpoint, behaviors);
            Consumers.Add(consumer);
            return consumer;
        }

        public class ProducedMessage
        {
            public ProducedMessage(byte[] message, IEnumerable<MessageHeader> headers, IEndpoint endpoint)
            {
                Message = message;

                if (headers != null)
                    Headers.AddRange(headers);

                Endpoint = endpoint;
            }

            public byte[] Message { get; }
            public MessageHeaderCollection Headers { get; } = new MessageHeaderCollection();
            public IEndpoint Endpoint { get; }
        }
    }
}