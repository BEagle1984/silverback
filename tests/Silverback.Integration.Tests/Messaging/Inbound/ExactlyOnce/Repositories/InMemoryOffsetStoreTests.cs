// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Inbound.ExactlyOnce.Repositories;
using Silverback.Tests.Types;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Inbound.ExactlyOnce.Repositories;

public class InMemoryOffsetStoreTests
{
    [Fact]
    public async Task Store_ForDifferentEndpoints_AllOffsetsStored()
    {
        InMemoryOffsetStore store = new(new TransactionalDictionarySharedItems<string, IBrokerMessageOffset>());

        await store.StoreAsync(
            new TestOffset("key1", "1"),
            new TestConsumerConfiguration("endpoint1") { GroupId = "group1" });
        await store.StoreAsync(
            new TestOffset("key1", "1"),
            new TestConsumerConfiguration("endpoint2") { GroupId = "group1" });
        await store.StoreAsync(
            new TestOffset("key1", "1"),
            new TestConsumerConfiguration("endpoint3") { GroupId = "group1" });
        await store.CommitAsync();

        store.CommittedItemsCount.Should().Be(3);
    }

    [Fact]
    public async Task Store_ForDifferentConsumerGroups_AllOffsetsStored()
    {
        InMemoryOffsetStore store = new(new TransactionalDictionarySharedItems<string, IBrokerMessageOffset>());

        await store.StoreAsync(
            new TestOffset("key1", "1"),
            new TestConsumerConfiguration("endpoint1") { GroupId = "group1" });
        await store.StoreAsync(
            new TestOffset("key1", "1"),
            new TestConsumerConfiguration("endpoint1") { GroupId = "group2" });
        await store.StoreAsync(
            new TestOffset("key1", "1"),
            new TestConsumerConfiguration("endpoint1") { GroupId = "group3" });
        await store.CommitAsync();

        store.CommittedItemsCount.Should().Be(3);
    }

    [Fact]
    public async Task Store_ForDifferentPartitions_AllOffsetsStored()
    {
        InMemoryOffsetStore store = new(new TransactionalDictionarySharedItems<string, IBrokerMessageOffset>());

        await store.StoreAsync(
            new TestOffset("key1", "1"),
            new TestConsumerConfiguration("endpoint1") { GroupId = "group1" });
        await store.StoreAsync(
            new TestOffset("key2", "1"),
            new TestConsumerConfiguration("endpoint1") { GroupId = "group1" });
        await store.StoreAsync(
            new TestOffset("key3", "1"),
            new TestConsumerConfiguration("endpoint1") { GroupId = "group1" });
        await store.CommitAsync();

        store.CommittedItemsCount.Should().Be(3);
    }

    [Fact]
    public async Task Store_SameTopicPartitionAndGroup_OffsetIsReplaced()
    {
        InMemoryOffsetStore store = new(new TransactionalDictionarySharedItems<string, IBrokerMessageOffset>());

        await store.StoreAsync(
            new TestOffset("key1", "1"),
            new TestConsumerConfiguration("endpoint1") { GroupId = "group1" });
        await store.StoreAsync(
            new TestOffset("key1", "2"),
            new TestConsumerConfiguration("endpoint1") { GroupId = "group1" });
        await store.StoreAsync(
            new TestOffset("key1", "3"),
            new TestConsumerConfiguration("endpoint1") { GroupId = "group1" });
        await store.CommitAsync();

        store.CommittedItemsCount.Should().Be(1);
    }

    [Fact]
    public async Task GetLatestValue_WithMultipleOffsetsStored_CorrectOffsetReturned()
    {
        InMemoryOffsetStore store = new(new TransactionalDictionarySharedItems<string, IBrokerMessageOffset>());

        await store.StoreAsync(
            new TestOffset("key1", "1"),
            new TestConsumerConfiguration("endpoint1") { GroupId = "group1" });
        await store.StoreAsync(
            new TestOffset("key1", "2"),
            new TestConsumerConfiguration("endpoint1") { GroupId = "group1" });
        await store.StoreAsync(
            new TestOffset("key1", "3"),
            new TestConsumerConfiguration("endpoint1") { GroupId = "group1" });
        await store.StoreAsync(
            new TestOffset("key2", "1"),
            new TestConsumerConfiguration("endpoint1") { GroupId = "group1" });
        await store.StoreAsync(
            new TestOffset("key1", "1"),
            new TestConsumerConfiguration("endpoint2") { GroupId = "group1" });
        await store.StoreAsync(
            new TestOffset("key1", "1"),
            new TestConsumerConfiguration("endpoint1") { GroupId = "group2" });
        await store.CommitAsync();

        IBrokerMessageOffset? result = await store.GetLatestValueAsync(
            "key1",
            new TestConsumerConfiguration("endpoint1")
            {
                GroupId = "group1"
            });

        result.Should().NotBeNull();
        result!.Value.Should().Be("3");
    }

    [Fact]
    public async Task GetLatestValue_NotStoredOffsets_NullReturned()
    {
        InMemoryOffsetStore store = new(new TransactionalDictionarySharedItems<string, IBrokerMessageOffset>());

        await store.StoreAsync(
            new TestOffset("key1", "1"),
            new TestConsumerConfiguration("endpoint1") { GroupId = "group1" });
        await store.CommitAsync();

        IBrokerMessageOffset? result = await store.GetLatestValueAsync(
            "key2",
            new TestConsumerConfiguration("endpoint1")
            {
                GroupId = "group1"
            });

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLatestValue_CommittedOffsetsFromMultipleInstances_LastCommittedValueReturned()
    {
        TransactionalDictionarySharedItems<string, IBrokerMessageOffset> sharedList = new();

        InMemoryOffsetStore store = new(sharedList);
        await store.StoreAsync(
            new TestOffset("key1", "1"),
            new TestConsumerConfiguration("endpoint1") { GroupId = "group1" });
        await store.CommitAsync();

        store = new InMemoryOffsetStore(sharedList);
        await store.StoreAsync(
            new TestOffset("key1", "2"),
            new TestConsumerConfiguration("endpoint1") { GroupId = "group1" });
        await store.CommitAsync();

        store = new InMemoryOffsetStore(sharedList);

        IBrokerMessageOffset? result = await store.GetLatestValueAsync(
            "key1",
            new TestConsumerConfiguration("endpoint1")
            {
                GroupId = "group1"
            });

        result.Should().NotBeNull();
        result!.Value.Should().Be("2");
    }

    [Fact]
    public async Task Rollback_Store_Reverted()
    {
        InMemoryOffsetStore store = new(new TransactionalDictionarySharedItems<string, IBrokerMessageOffset>());

        await store.StoreAsync(
            new TestOffset("key1", "1"),
            new TestConsumerConfiguration("endpoint1") { GroupId = "group1" });
        await store.CommitAsync();

        store.CommittedItemsCount.Should().Be(1);

        await store.StoreAsync(
            new TestOffset("key1", "2"),
            new TestConsumerConfiguration("endpoint1") { GroupId = "group1" });
        await store.RollbackAsync();

        store.CommittedItemsCount.Should().Be(1);

        IBrokerMessageOffset? result = await store.GetLatestValueAsync(
            "key1",
            new TestConsumerConfiguration("endpoint1")
            {
                GroupId = "group1"
            });

        result.Should().NotBeNull();
        result!.Value.Should().Be("1");
    }
}
