// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
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
                new MessageLogger())
        {
            ProducedMessages = broker.ProducedMessages;
        }

        protected override IOffset Produce(object message, byte[] serializedMessage, IEnumerable<MessageHeader> headers)
        {
            ProducedMessages.Add(new TestBroker.ProducedMessage(serializedMessage, headers, Endpoint));
            return null;
        }

        protected override Task<IOffset> ProduceAsync(object message, byte[] serializedMessage, IEnumerable<MessageHeader> headers)
        {
            Produce(message, serializedMessage, headers);
            return Task.FromResult<IOffset>(null);
        }
    }
}