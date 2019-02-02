// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.LargeMessages;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.TestTypes
{
    public class TestProducer : Producer
    {
        public List<TestBroker.ProducedMessage> ProducedMessages { get; }

        public TestProducer(TestBroker broker, IEndpoint endpoint)
            : base(broker, endpoint,
                new MessageKeyProvider(new[] {new DefaultPropertiesMessageKeyProvider()}),
                new ChunkProducer(),
                new NullLogger<TestProducer>(),
                new MessageLogger(new MessageKeyProvider(Enumerable.Empty<IMessageKeyProvider>())))
        {
            ProducedMessages = broker.ProducedMessages;
        }

        protected override void Produce(object message, byte[] serializedMessage)
        {
            ProducedMessages.Add(new TestBroker.ProducedMessage(serializedMessage, Endpoint));
        }

        protected override Task ProduceAsync(object message, byte[] serializedMessage)
        {
            Produce(message, serializedMessage);
            return Task.CompletedTask;
        }
    }
}