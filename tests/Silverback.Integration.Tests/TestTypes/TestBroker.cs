// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Tests.Types;

namespace Silverback.Tests.Integration.TestTypes;

public class TestBroker : Broker<TestProducerConfiguration, TestConsumerConfiguration>
{
    public TestBroker(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    public IList<ProducedMessage> ProducedMessages { get; } = new List<ProducedMessage>();

    public bool SimulateConnectIssues { get; set; }

    protected override Task ConnectAsync(IEnumerable<IBrokerConnectedObject> connectedObjects)
    {
        if (SimulateConnectIssues)
        {
            SimulateConnectIssues = false;
            throw new IOException("Simulated exception.");
        }

        return base.ConnectAsync(connectedObjects);
    }

    protected override IProducer InstantiateProducer(
        TestProducerConfiguration configuration,
        IBrokerBehaviorsProvider<IProducerBehavior> behaviorsProvider,
        IServiceProvider serviceProvider) =>
        new TestProducer(
            this,
            configuration,
            behaviorsProvider,
            serviceProvider.GetRequiredService<IOutboundEnvelopeFactory>(),
            serviceProvider);

    protected override IConsumer InstantiateConsumer(
        TestConsumerConfiguration configuration,
        IBrokerBehaviorsProvider<IConsumerBehavior> behaviorsProvider,
        IServiceProvider serviceProvider) =>
        new TestConsumer(this, configuration, behaviorsProvider, serviceProvider);
}
