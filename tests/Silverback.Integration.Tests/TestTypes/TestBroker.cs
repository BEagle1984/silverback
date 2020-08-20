// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;

namespace Silverback.Tests.Integration.TestTypes
{
    public class TestBroker : Broker<TestProducerEndpoint, TestConsumerEndpoint>
    {
        public TestBroker(IServiceProvider serviceProvider, IEnumerable<IBrokerBehavior> behaviors)
            : base(behaviors, serviceProvider)
        {
        }

        public List<ProducedMessage> ProducedMessages { get; } = new List<ProducedMessage>();

        public bool SimulateConnectIssues { get; set; }

        protected override void Connect(IEnumerable<IConsumer> consumers)
        {
            if (SimulateConnectIssues)
            {
                SimulateConnectIssues = false;
                throw new IOException("Simulated exception.");
            }

            base.Connect(consumers);
        }

        protected override IProducer InstantiateProducer(
            TestProducerEndpoint endpoint,
            IReadOnlyList<IProducerBehavior>? behaviors,
            IServiceProvider serviceProvider) =>
            new TestProducer(this, endpoint, behaviors);

        protected override IConsumer InstantiateConsumer(
            TestConsumerEndpoint endpoint,
            MessagesReceivedAsyncCallback callback,
            IReadOnlyList<IConsumerBehavior>? behaviors,
            IServiceProvider serviceProvider) =>
            new TestConsumer(
                this,
                endpoint,
                callback,
                behaviors,
                serviceProvider,
                serviceProvider.GetRequiredService<ISilverbackIntegrationLogger<TestConsumer>>());
    }
}
