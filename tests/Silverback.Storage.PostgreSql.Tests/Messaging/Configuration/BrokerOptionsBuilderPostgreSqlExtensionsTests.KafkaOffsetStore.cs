// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Storage.PostgreSql.Messaging.Configuration;

public partial class BrokerOptionsBuilderPostgreSqlExtensionsTests
{
    [Fact]
    public void AddPostgreSqlKafkaOffsetStore_ShouldConfigureOffsetStoreFactories()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddKafka().AddPostgreSqlKafkaOffsetStore()));

        IKafkaOffsetStoreFactory factory = serviceProvider.GetRequiredService<IKafkaOffsetStoreFactory>();

        IKafkaOffsetStore store = factory.GetStore(new PostgreSqlKafkaOffsetStoreSettings("conn"), serviceProvider);

        store.ShouldBeOfType<PostgreSqlKafkaOffsetStore>();
    }

    [Fact]
    public void AddPostgreSqlKafkaOffsetStore_ShouldThrow_WhenAddKafkaHasNotBeenCalled()
    {
        BrokerOptionsBuilder builder = new(new SilverbackBuilder(new ServiceCollection()));

        Action act = () => builder.AddPostgreSqlKafkaOffsetStore();

        act.ShouldThrow<InvalidOperationException>().Message.ShouldBe("OffsetStoreFactory not found, AddKafka has not been called.");
    }

    [Fact]
    public void AddPostgreSqlKafkaOffsetStore_ShouldKeepRegisteredFactory_WhenFactoryIsAlreadyRegistered()
    {
        IKafkaOffsetStore customStore = Substitute.For<IKafkaOffsetStore>();
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker(options =>
            {
                options.AddKafka();
                GetSingletonInstance<KafkaOffsetStoreFactory>(services)
                    .AddFactory<PostgreSqlKafkaOffsetStoreSettings>((_, _) => customStore);
                options.AddPostgreSqlKafkaOffsetStore();
            }));

        IKafkaOffsetStoreFactory factory = serviceProvider.GetRequiredService<IKafkaOffsetStoreFactory>();

        IKafkaOffsetStore store = factory.GetStore(new PostgreSqlKafkaOffsetStoreSettings("conn"), serviceProvider);

        store.ShouldBeSameAs(customStore);
    }
}
