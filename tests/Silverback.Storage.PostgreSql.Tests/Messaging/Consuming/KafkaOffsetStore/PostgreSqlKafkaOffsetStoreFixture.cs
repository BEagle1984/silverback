// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
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
        _offsetStoreSettings = new PostgreSqlKafkaOffsetStoreSettings(ConnectionString, "TestOutbox");
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
        PostgreSqlKafkaOffsetStore store = (PostgreSqlKafkaOffsetStore)factory.GetStore(_offsetStoreSettings);

        await store.StoreOffsetsAsync(
            "group1",
            new[]
            {
                new KafkaOffset("topic1", 0, 42),
                new KafkaOffset("topic1", 1, 42)
            });
        await store.StoreOffsetsAsync(
            "group2",
            new[]
            {
                new KafkaOffset("topic1", 0, 42)
            });

        IReadOnlyCollection<KafkaOffset> offsets = store.GetStoredOffsets("group1");

        offsets.Should().HaveCount(2);
        offsets.Should().BeEquivalentTo(
            new[]
            {
                new KafkaOffset("topic1", 0, 42),
                new KafkaOffset("topic1", 1, 42)
            },
            options => options.WithoutStrictOrdering());
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
        IKafkaOffsetStore store = factory.GetStore(_offsetStoreSettings);

        KafkaOffset[] offsets =
        {
            new("topic1", 3, 42),
            new("topic1", 5, 42)
        };

        await store.StoreOffsetsAsync("group1", offsets);

        IReadOnlyCollection<KafkaOffset> storedOffsets = store.GetStoredOffsets("group1");
        storedOffsets.Should().HaveCount(2);
        storedOffsets.Should().BeEquivalentTo(offsets, options => options.WithStrictOrdering());
    }
}
