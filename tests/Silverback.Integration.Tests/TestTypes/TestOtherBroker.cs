// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Outbound.Routing;

namespace Silverback.Tests.Integration.TestTypes;

public class TestOtherBroker : Broker<TestOtherProducerConfiguration, TestOtherConsumerConfiguration>
{
    public TestOtherBroker(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    public IList<ProducedMessage> ProducedMessages { get; } = new List<ProducedMessage>();

    protected override IProducer InstantiateProducer(
        TestOtherProducerConfiguration configuration,
        IBrokerBehaviorsProvider<IProducerBehavior> behaviorsProvider,
        IServiceProvider serviceProvider) =>
        new TestOtherProducer(
            this,
            configuration,
            behaviorsProvider,
            serviceProvider.GetRequiredService<IOutboundEnvelopeFactory>(),
            serviceProvider);

    protected override IConsumer InstantiateConsumer(
        TestOtherConsumerConfiguration configuration,
        IBrokerBehaviorsProvider<IConsumerBehavior> behaviorsProvider,
        IServiceProvider serviceProvider) =>
        new TestOtherConsumer(this, configuration, behaviorsProvider, serviceProvider);
}
