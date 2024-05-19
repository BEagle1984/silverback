// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using NSubstitute;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Tests.Integration.Kafka.TestTypes;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker.Kafka;

public class ConfluentProducerWrapperFactoryFixture
{
    private readonly IBrokerClientCallbacksInvoker _callbacksInvoker = Substitute.For<IBrokerClientCallbacksInvoker>();

    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();

    private readonly ISilverbackLoggerFactory _loggerFactory = new SilverbackLoggerFactorySubstitute();

    public ConfluentProducerWrapperFactoryFixture()
    {
        _serviceProvider.GetService(typeof(IConfluentProducerBuilder)).Returns(Substitute.For<IConfluentProducerBuilder>());
    }

    [Fact]
    public void Create_ShouldCreateProducerWrapper()
    {
        ConfluentProducerWrapperFactory factory = new(_callbacksInvoker, _serviceProvider, _loggerFactory);

        IConfluentProducerWrapper producer = factory.Create("test", new KafkaProducerConfiguration());

        producer.Should().BeOfType<ConfluentProducerWrapper>();
    }
}
