// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.TestTypes
{
    public class TestProducer : Producer
    {
        public List<TestBroker.ProducedMessage> ProducedMessages { get; }

        public TestProducer(TestBroker broker, IEndpoint endpoint)
            : base(broker, endpoint,
                new MessageKeyProvider(new[] {new DefaultPropertiesMessageKeyProvider()}),
                new NullLogger<TestProducer>(),
                new MessageLogger(new MessageKeyProvider(Enumerable.Empty<IMessageKeyProvider>())))
        {
            ProducedMessages = broker.ProducedMessages;
        }

        protected override void Produce(object message, byte[] serializedMessage, IEnumerable<MessageHeader> headers)
        {
            ProducedMessages.Add(new TestBroker.ProducedMessage(serializedMessage, headers, Endpoint));
        }

        protected override Task ProduceAsync(object message, byte[] serializedMessage, IEnumerable<MessageHeader> headers)
        {
            Produce(message, serializedMessage, headers);
            return Task.CompletedTask;
        }
    }
}