// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Storage.EntityFramework.Messaging.Configuration;

public partial class BrokerOptionsBuilderEntityFrameworkExtensionsTests
{
    [Fact]
    public void AddEntityFrameworkOutbox_ShouldConfigureOutboxFactories()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddEntityFrameworkOutbox()));

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<IOutboxWriterFactory>();

        OutboxSettings outboxSettings = new EntityFrameworkOutboxSettings(typeof(TestDbContext), GetDbContext);

        IOutboxReader reader = readerFactory.GetReader(outboxSettings, serviceProvider);
        IOutboxWriter writer = writerFactory.GetWriter(outboxSettings, serviceProvider);

        reader.ShouldBeOfType<EntityFrameworkOutboxReader>();
        writer.ShouldBeOfType<EntityFrameworkOutboxWriter>();
    }

    [Fact]
    public void AddEntityFrameworkOutbox_ShouldNotThrow_WhenCalledTwice()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddEntityFrameworkOutbox().AddEntityFrameworkOutbox()));

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<IOutboxWriterFactory>();

        OutboxSettings outboxSettings = new EntityFrameworkOutboxSettings(typeof(TestDbContext), GetDbContext);

        readerFactory.GetReader(outboxSettings, serviceProvider).ShouldBeOfType<EntityFrameworkOutboxReader>();
        writerFactory.GetWriter(outboxSettings, serviceProvider).ShouldBeOfType<EntityFrameworkOutboxWriter>();
    }

    [Fact]
    public void AddEntityFrameworkOutbox_ShouldThrow_WhenOutboxFactoriesAreNotRegistered()
    {
        BrokerOptionsBuilder builder = new(new SilverbackBuilder(new ServiceCollection()));

        Action act = () => builder.AddEntityFrameworkOutbox();

        act.ShouldThrow<InvalidOperationException>().Message.ShouldContain("WithConnectionToMessageBroker has not been called");
    }

    [Fact]
    public void AddEntityFrameworkOutbox_ShouldThrow_WhenOutboxWriterFactoryIsNotRegistered()
    {
        ServiceCollection services = [];
        services.AddSingleton(new OutboxReaderFactory());
        BrokerOptionsBuilder builder = new(new SilverbackBuilder(services));

        Action act = () => builder.AddEntityFrameworkOutbox();

        act.ShouldThrow<InvalidOperationException>().Message.ShouldContain("WithConnectionToMessageBroker has not been called");
    }
}
