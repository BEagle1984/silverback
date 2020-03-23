// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.Broker;

namespace Silverback.Tests.Integration.TestTypes
{
    public class TestOtherBroker : Broker<TestOtherProducerEndpoint, TestOtherConsumerEndpoint>
    {
        public TestOtherBroker(IEnumerable<IBrokerBehavior> behaviors = null)
            : base(behaviors, NullLoggerFactory.Instance)
        {
        }

        public List<TestOtherConsumer> Consumers { get; } = new List<TestOtherConsumer>();

        public List<ProducedMessage> ProducedMessages { get; } = new List<ProducedMessage>();

        protected override IProducer InstantiateProducer(
            TestOtherProducerEndpoint endpoint,
            IEnumerable<IProducerBehavior> behaviors) =>
            new TestOtherProducer(this, endpoint, behaviors);

        protected override IConsumer InstantiateConsumer(
            TestOtherConsumerEndpoint endpoint,
            IEnumerable<IConsumerBehavior> behaviors)
        {
            var consumer = new TestOtherConsumer(
                this,
                endpoint,
                behaviors,
                LoggerFactory.CreateLogger<TestOtherConsumer>());

            Consumers.Add(consumer);
            return consumer;
        }
    }
}