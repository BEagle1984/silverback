// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Tests.Types;

namespace Silverback.Tests.Integration.TestTypes
{
    public class TestBroker : Broker<TestProducerEndpoint, TestConsumerEndpoint>
    {
        public TestBroker(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        public IList<ProducedMessage> ProducedMessages { get; } = new List<ProducedMessage>();

        public bool SimulateConnectIssues { get; set; }

        protected override Task ConnectAsync(IEnumerable<IConsumer> consumers)
        {
            if (SimulateConnectIssues)
            {
                SimulateConnectIssues = false;
                throw new IOException("Simulated exception.");
            }

            return base.ConnectAsync(consumers);
        }

        protected override IProducer InstantiateProducer(
            TestProducerEndpoint endpoint,
            IBrokerBehaviorsProvider<IProducerBehavior> behaviorsProvider,
            IServiceProvider serviceProvider) =>
            new TestProducer(this, endpoint, behaviorsProvider, serviceProvider);

        protected override IConsumer InstantiateConsumer(
            TestConsumerEndpoint endpoint,
            IBrokerBehaviorsProvider<IConsumerBehavior> behaviorsProvider,
            IServiceProvider serviceProvider) =>
            new TestConsumer(this, endpoint, behaviorsProvider, serviceProvider);
    }
}
