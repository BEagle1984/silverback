// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Storage.EntityFramework.Messaging.Configuration;

public partial class BrokerOptionsBuilderEntityFrameworkExtensionsTests
{
    [Fact]
    public void AddEntityFrameworkKafkaOffsetStore_ShouldConfigureOffsetStoreFactories()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddKafka().AddEntityFrameworkKafkaOffsetStore()));

        IKafkaOffsetStoreFactory factory = serviceProvider.GetRequiredService<IKafkaOffsetStoreFactory>();

        IKafkaOffsetStore store = factory.GetStore(
            new EntityFrameworkKafkaOffsetStoreSettings(typeof(TestDbContext), GetDbContext),
            serviceProvider);

        store.ShouldBeOfType<EntityFrameworkKafkaOffsetStore>();
    }

    [Fact]
    public void AddEntityFrameworkKafkaOffsetStore_ShouldNotThrow_WhenCalledTwice()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddKafka()
                .AddEntityFrameworkKafkaOffsetStore()
                .AddEntityFrameworkKafkaOffsetStore()));

        IKafkaOffsetStoreFactory factory = serviceProvider.GetRequiredService<IKafkaOffsetStoreFactory>();

        IKafkaOffsetStore store = factory.GetStore(
            new EntityFrameworkKafkaOffsetStoreSettings(typeof(TestDbContext), GetDbContext),
            serviceProvider);

        store.ShouldBeOfType<EntityFrameworkKafkaOffsetStore>();
    }

    [Fact]
    public void AddEntityFrameworkKafkaOffsetStore_ShouldThrow_WhenKafkaHasNotBeenAdded()
    {
        BrokerOptionsBuilder builder = new(new SilverbackBuilder(new ServiceCollection()));

        Action act = () => builder.AddEntityFrameworkKafkaOffsetStore();

        act.ShouldThrow<InvalidOperationException>().Message.ShouldContain("AddKafka has not been called");
    }
}
