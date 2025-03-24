// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Storage;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Storage.PostgreSql.Messaging.Consuming.KafkaOffsetStore;

public sealed class PostgreSqlKafkaOffsetStoreFixture : PostgresContainerFixture
{
    private readonly PostgreSqlKafkaOffsetStoreSettings _offsetStoreSettings;

    public PostgreSqlKafkaOffsetStoreFixture()
    {
        _offsetStoreSettings = new PostgreSqlKafkaOffsetStoreSettings(ConnectionString);
    }

    [Fact]
    public async Task GetStoredOffsets_ShouldReturnStoredOffsetsForGroup()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddKafka().AddPostgreSqlKafkaOffsetStore()));

        SilverbackStorageInitializer storageInitializer = serviceProvider.GetRequiredService<SilverbackStorageInitializer>();
        await storageInitializer.CreatePostgreSqlKafkaOffsetStoreAsync(_offsetStoreSettings);

        IKafkaOffsetStoreFactory factory = serviceProvider.GetRequiredService<IKafkaOffsetStoreFactory>();
        PostgreSqlKafkaOffsetStore store = (PostgreSqlKafkaOffsetStore)factory.GetStore(_offsetStoreSettings, serviceProvider);

        await store.StoreOffsetsAsync(
            "group1",
            [
                new KafkaOffset("topic1", 0, 42),
                new KafkaOffset("topic1", 1, 42)
            ]);
        await store.StoreOffsetsAsync(
            "group2",
            [
                new KafkaOffset("topic1", 0, 42)
            ]);

        IReadOnlyCollection<KafkaOffset> offsets = store.GetStoredOffsets("group1");

        offsets.Count.ShouldBe(2);
        offsets.ShouldBe(
            [
                new KafkaOffset("topic1", 0, 42),
                new KafkaOffset("topic1", 1, 42)
            ],
            ignoreOrder: true);
    }

    [Fact]
    public async Task StoreOffsetsAsync_ShouldStoreOffsets()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddKafka().AddPostgreSqlKafkaOffsetStore()));

        SilverbackStorageInitializer storageInitializer = serviceProvider.GetRequiredService<SilverbackStorageInitializer>();
        await storageInitializer.CreatePostgreSqlKafkaOffsetStoreAsync(_offsetStoreSettings);

        IKafkaOffsetStoreFactory factory = serviceProvider.GetRequiredService<IKafkaOffsetStoreFactory>();
        IKafkaOffsetStore store = factory.GetStore(_offsetStoreSettings, serviceProvider);

        KafkaOffset[] offsets =
        [
            new("topic1", 3, 42),
            new("topic1", 5, 42)
        ];

        await store.StoreOffsetsAsync("group1", offsets);

        IReadOnlyCollection<KafkaOffset> storedOffsets = store.GetStoredOffsets("group1");
        storedOffsets.Count.ShouldBe(2);
        storedOffsets.ShouldBe(offsets);
    }
}
