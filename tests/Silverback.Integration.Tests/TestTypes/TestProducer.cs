// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.TestTypes
{
    public class TestProducer : Producer<TestBroker, TestProducerEndpoint>
    {
        public List<TestBroker.ProducedMessage> ProducedMessages { get; }

        public TestProducer(TestBroker broker, TestProducerEndpoint endpoint, IEnumerable<IProducerBehavior> behaviors)
            : base(
                broker,
                endpoint,
                new MessageKeyProvider(new[] {new DefaultPropertiesMessageKeyProvider()}),
                behaviors,
                new NullLogger<TestProducer>(),
                new MessageLogger())
        {
            ProducedMessages = broker.ProducedMessages;
        }

        protected override IOffset Produce(RawBrokerMessage message)
        {
            ProducedMessages.Add(new TestBroker.ProducedMessage(message.RawContent, message.Headers, Endpoint));
            return null;
        }

        protected override Task<IOffset> ProduceAsync(RawBrokerMessage message)
        {
            Produce(message.RawContent, message.Headers);
            return Task.FromResult<IOffset>(null);
        }
    }
}