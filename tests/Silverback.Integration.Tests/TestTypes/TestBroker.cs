// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;

namespace Silverback.Tests.Integration.TestTypes
{
    public class TestBroker : Broker<TestProducerEndpoint, TestConsumerEndpoint>
    {
        public TestBroker(IEnumerable<IBrokerBehavior> behaviors = null)
            : base(behaviors, NullLoggerFactory.Instance)
        {
        }

        public List<TestConsumer> Consumers { get; } = new List<TestConsumer>();

        public List<ProducedMessage> ProducedMessages { get; } = new List<ProducedMessage>();

        protected override IProducer InstantiateProducer(
            TestProducerEndpoint endpoint,
            IEnumerable<IProducerBehavior> behaviors) =>
            new TestProducer(this, endpoint, behaviors);

        protected override IConsumer InstantiateConsumer(
            TestConsumerEndpoint endpoint,
            IEnumerable<IConsumerBehavior> behaviors)
        {
            var consumer = new TestConsumer(
                this,
                endpoint,
                behaviors,
                LoggerFactory.CreateLogger<TestConsumer>());

            Consumers.Add(consumer);
            return consumer;
        }
    }
}