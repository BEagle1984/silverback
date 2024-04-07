// Copyright (c) 2024 Sergio Aquilini
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
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Storage.Memory.Messaging.Consuming.KafkaOffsetStore;

public class InMemoryKafkaOffsetStoreFixture
{
    [Fact]
    public async Task GetStoredOffsets_ShouldReturnStoredOffsetsForGroup()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddKafka().AddInMemoryKafkaOffsetStore()));

        InMemoryKafkaOffsetStoreSettings kafkaOffsetStoreSettings = new();
        IKafkaOffsetStoreFactory factory = serviceProvider.GetRequiredService<IKafkaOffsetStoreFactory>();
        InMemoryKafkaOffsetStore store = (InMemoryKafkaOffsetStore)factory.GetStore(kafkaOffsetStoreSettings, serviceProvider);

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

        offsets.Should().HaveCount(2);
        offsets.Should().BeEquivalentTo(
            [
                new KafkaOffset("topic1", 0, 42),
                new KafkaOffset("topic1", 1, 42)
            ],
            options => options.WithoutStrictOrdering());
    }

    [Fact]
    public async Task StoreOffsetsAsync_ShouldStoreOffsets()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddKafka().AddInMemoryKafkaOffsetStore()));

        InMemoryKafkaOffsetStoreSettings kafkaOffsetStoreSettings = new();
        IKafkaOffsetStoreFactory factory = serviceProvider.GetRequiredService<IKafkaOffsetStoreFactory>();
        IKafkaOffsetStore store = factory.GetStore(kafkaOffsetStoreSettings, serviceProvider);

        KafkaOffset[] offsets =
        [
            new KafkaOffset("topic1", 3, 42),
            new KafkaOffset("topic1", 5, 42)
        ];

        await store.StoreOffsetsAsync("group1", offsets);

        IReadOnlyCollection<KafkaOffset> storedOffsets = store.GetStoredOffsets("group1");
        storedOffsets.Should().HaveCount(2);
        storedOffsets.Should().BeEquivalentTo(offsets, options => options.WithStrictOrdering());
    }
}
