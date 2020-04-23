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
    public class TestOtherBroker : Broker<TestOtherProducerEndpoint, TestOtherConsumerEndpoint>
    {
        public TestOtherBroker(IServiceProvider serviceProvider, IEnumerable<IBrokerBehavior> behaviors)
            : base(behaviors, NullLoggerFactory.Instance, serviceProvider)
        {
        }

        public List<ProducedMessage> ProducedMessages { get; } = new List<ProducedMessage>();

        protected override IProducer InstantiateProducer(
            TestOtherProducerEndpoint endpoint,
            IReadOnlyCollection<IProducerBehavior> behaviors,
            IServiceProvider serviceProvider) =>
            new TestOtherProducer(this, endpoint, behaviors);

        protected override IConsumer InstantiateConsumer(
            TestOtherConsumerEndpoint endpoint,
            MessagesReceivedAsyncCallback callback,
            IReadOnlyCollection<IConsumerBehavior> behaviors,
            IServiceProvider serviceProvider) =>
            new TestOtherConsumer(
                this,
                endpoint,
                callback,
                behaviors,
                serviceProvider,
                LoggerFactory.CreateLogger<TestOtherConsumer>());
    }
}