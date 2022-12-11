// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration;

public class SilverbackBuilderWithConnectionToExtensionsFixture
{
    [Fact]
    public void WithConnectionToMessageBroker_ShouldRegisterClientsCollections()
    {
        ServiceCollection serviceCollection = new();

        serviceCollection
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker();

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

        serviceProvider.GetService<BrokerClientCollection>().Should().NotBeNull();
        serviceProvider.GetService<IProducerCollection>().Should().NotBeNull();
        serviceProvider.GetService<IConsumerCollection>().Should().NotBeNull();
    }

    [Fact]
    public void WithConnectionToMessageBroker_ShouldRegisterDefaultOutboxFactories()
    {
        ServiceCollection serviceCollection = new();

        serviceCollection
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker();

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        OutboxReaderFactory defaultReaderFactory = serviceProvider.GetRequiredService<OutboxReaderFactory>();
        IOutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<IOutboxWriterFactory>();
        OutboxWriterFactory defaultWriterFactory = serviceProvider.GetRequiredService<OutboxWriterFactory>();

        readerFactory.Should().NotBeNull();
        writerFactory.Should().NotBeNull();

        readerFactory.Should().BeSameAs(defaultReaderFactory);
        writerFactory.Should().BeSameAs(defaultWriterFactory);
    }
}
