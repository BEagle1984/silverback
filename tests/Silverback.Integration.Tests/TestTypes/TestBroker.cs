// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.TestTypes
{
    public class TestBroker : Broker
    {
        public TestBroker() : base(NullLoggerFactory.Instance)
        {
        }

        public List<TestConsumer> Consumers { get; } = new List<TestConsumer>();

        public List<ProducedMessage> ProducedMessages { get; } = new List<ProducedMessage>();

        protected override Producer InstantiateProducer(IEndpoint endpoint) => new TestProducer(this, endpoint);

        protected override Consumer InstantiateConsumer(IEndpoint endpoint)
        {
            var consumer = new TestConsumer(this, endpoint);
            Consumers.Add(consumer);
            return consumer;
        }

        protected override void Connect(IEnumerable<IConsumer> consumers)
        {
            consumers.Cast<TestConsumer>().ToList().ForEach(c => c.IsReady = true);
        }

        protected override void Disconnect(IEnumerable<IConsumer> consumers)
        {
        }

        public class ProducedMessage
        {
            public ProducedMessage(byte[] message, IEnumerable<MessageHeader> headers, IEndpoint endpoint)
            {
                Message = message;
                Headers = headers;
                Endpoint = endpoint;
            }

            public byte[] Message { get; }
            public IEnumerable<MessageHeader> Headers { get; }
            public IEndpoint Endpoint { get; }
        }
    }
}
