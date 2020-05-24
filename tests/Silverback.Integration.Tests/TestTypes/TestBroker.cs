// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;

namespace Silverback.Tests.Integration.TestTypes
{
    public class TestBroker : Broker<TestProducerEndpoint, TestConsumerEndpoint>
    {
        public TestBroker(IServiceProvider serviceProvider, IEnumerable<IBrokerBehavior> behaviors)
            : base(behaviors, NullLoggerFactory.Instance, serviceProvider)
        {
        }

        public List<ProducedMessage> ProducedMessages { get; } = new List<ProducedMessage>();

        protected override IProducer InstantiateProducer(
            TestProducerEndpoint endpoint,
            IReadOnlyCollection<IProducerBehavior>? behaviors,
            IServiceProvider serviceProvider) =>
            new TestProducer(this, endpoint, behaviors);

        protected override IConsumer InstantiateConsumer(
            TestConsumerEndpoint endpoint,
            MessagesReceivedAsyncCallback callback,
            IReadOnlyCollection<IConsumerBehavior>? behaviors,
            IServiceProvider serviceProvider) =>
            new TestConsumer(
                this,
                endpoint,
                callback,
                behaviors,
                serviceProvider,
                LoggerFactory.CreateLogger<TestConsumer>());
    }
}