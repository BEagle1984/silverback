// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection;
using Shouldly;
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
        ServiceCollection serviceCollection = [];

        serviceCollection
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker();

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

        serviceProvider.GetService<BrokerClientCollection>().ShouldNotBeNull();
        serviceProvider.GetService<IProducerCollection>().ShouldNotBeNull();
        serviceProvider.GetService<IConsumerCollection>().ShouldNotBeNull();
    }

    [Fact]
    public void WithConnectionToMessageBroker_ShouldRegisterDefaultOutboxFactories()
    {
        ServiceCollection serviceCollection = [];

        serviceCollection
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker();

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        OutboxReaderFactory defaultReaderFactory = serviceProvider.GetRequiredService<OutboxReaderFactory>();
        IOutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<IOutboxWriterFactory>();
        OutboxWriterFactory defaultWriterFactory = serviceProvider.GetRequiredService<OutboxWriterFactory>();

        readerFactory.ShouldNotBeNull();
        writerFactory.ShouldNotBeNull();

        readerFactory.ShouldBeSameAs(defaultReaderFactory);
        writerFactory.ShouldBeSameAs(defaultWriterFactory);
    }
}
