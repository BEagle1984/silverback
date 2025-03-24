// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Producing;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Types;

namespace Silverback.Tests.Integration.Messaging.Publishing;

public partial class IntegrationPublisherExtensionsFixture
{
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    private readonly ProducerCollection _producers = [];

    public IntegrationPublisherExtensionsFixture()
    {
        IServiceProvider serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IProducerCollection)).Returns(_producers);
        _publisher.Context.Returns(new SilverbackContext(serviceProvider));
    }

    private (IProducer Producer, IProduceStrategyImplementation Strategy) AddProducer<TMessage>(string topic, bool enableSubscribing = false)
    {
        IProducer producer = Substitute.For<IProducer>();
        producer.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration(topic, typeof(TMessage))
            {
                Strategy = Substitute.For<IProduceStrategy>(),
                EnableSubscribing = enableSubscribing
            });
        IProduceStrategyImplementation produceStrategyImplementation = Substitute.For<IProduceStrategyImplementation>();
        producer.EndpointConfiguration.Strategy.Build(
            Arg.Any<ISilverbackContext>(),
            Arg.Any<ProducerEndpointConfiguration>()).Returns(produceStrategyImplementation);
        _producers.Add(producer);
        return (producer, produceStrategyImplementation);
    }
}
