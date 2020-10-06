// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Sequences;

namespace Silverback.Tests.Integration.TestTypes
{
    public class TestOtherBroker : Broker<TestOtherProducerEndpoint, TestOtherConsumerEndpoint>
    {
        public TestOtherBroker(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        public IList<ProducedMessage> ProducedMessages { get; } = new List<ProducedMessage>();

        protected override IProducer InstantiateProducer(
            TestOtherProducerEndpoint endpoint,
            IBrokerBehaviorsProvider<IProducerBehavior> behaviorsProvider,
            IServiceProvider serviceProvider) =>
            new TestOtherProducer(this, endpoint, behaviorsProvider, serviceProvider);

        protected override IConsumer InstantiateConsumer(
            TestOtherConsumerEndpoint endpoint,
            IBrokerBehaviorsProvider<IConsumerBehavior> behaviorsProvider,
            IServiceProvider serviceProvider) =>
            new TestOtherConsumer(this, endpoint, behaviorsProvider, serviceProvider);
    }
}
