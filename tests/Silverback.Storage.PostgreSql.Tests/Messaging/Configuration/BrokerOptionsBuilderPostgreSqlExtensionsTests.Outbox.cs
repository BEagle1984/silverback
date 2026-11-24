// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Storage.PostgreSql.Messaging.Configuration;

public partial class BrokerOptionsBuilderPostgreSqlExtensionsTests
{
    [Fact]
    public void AddPostgreSqlOutbox_ShouldConfigureOutboxFactories()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddPostgreSqlOutbox()));

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<IOutboxWriterFactory>();

        OutboxSettings outboxSettings = new PostgreSqlOutboxSettings("conn");

        IOutboxReader reader = readerFactory.GetReader(outboxSettings, serviceProvider);
        IOutboxWriter writer = writerFactory.GetWriter(outboxSettings, serviceProvider);

        reader.ShouldBeOfType<PostgreSqlOutboxReader>();
        writer.ShouldBeOfType<PostgreSqlOutboxWriter>();
    }

    [Fact]
    public void AddPostgreSqlOutbox_ShouldThrow_WhenOutboxReaderFactoryIsNotRegistered()
    {
        ServiceCollection services = [];
        services.AddSingleton(new OutboxWriterFactory());
        BrokerOptionsBuilder builder = new(new SilverbackBuilder(services));

        Action act = () => builder.AddPostgreSqlOutbox();

        act.ShouldThrow<InvalidOperationException>().Message.ShouldBe(
            "OutboxReaderFactory/OutboxWriterFactory not found, WithConnectionToMessageBroker has not been called.");
    }

    [Fact]
    public void AddPostgreSqlOutbox_ShouldThrow_WhenOutboxWriterFactoryIsNotRegistered()
    {
        ServiceCollection services = [];
        services.AddSingleton(new OutboxReaderFactory());
        BrokerOptionsBuilder builder = new(new SilverbackBuilder(services));

        Action act = () => builder.AddPostgreSqlOutbox();

        act.ShouldThrow<InvalidOperationException>().Message.ShouldBe(
            "OutboxReaderFactory/OutboxWriterFactory not found, WithConnectionToMessageBroker has not been called.");
    }

    [Fact]
    public void AddPostgreSqlOutbox_ShouldKeepRegisteredFactories_WhenFactoriesAreAlreadyRegistered()
    {
        IOutboxReader customReader = Substitute.For<IOutboxReader>();
        IOutboxWriter customWriter = Substitute.For<IOutboxWriter>();
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker(options =>
            {
                GetSingletonInstance<OutboxReaderFactory>(services).AddFactory<PostgreSqlOutboxSettings>((_, _) => customReader);
                GetSingletonInstance<OutboxWriterFactory>(services).AddFactory<PostgreSqlOutboxSettings>((_, _) => customWriter);
                options.AddPostgreSqlOutbox();
            }));

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<IOutboxWriterFactory>();

        OutboxSettings outboxSettings = new PostgreSqlOutboxSettings("conn");

        IOutboxReader reader = readerFactory.GetReader(outboxSettings, serviceProvider);
        IOutboxWriter writer = writerFactory.GetWriter(outboxSettings, serviceProvider);

        reader.ShouldBeSameAs(customReader);
        writer.ShouldBeSameAs(customWriter);
    }

    private static T GetSingletonInstance<T>(IServiceCollection services) =>
        services.Single(descriptor => descriptor.ServiceType == typeof(T)).ImplementationInstance.ShouldBeOfType<T>();
}
