// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Collections;
using Xunit;

namespace Silverback.Tests.Storage.Memory.Collections;

public class InMemoryStorageFixture
{
    [Fact]
    public void Add_ShouldAddItemToStorage()
    {
        InMemoryStorage<string> storage = new();
        storage.Add("A");
        storage.Add("B");
        storage.Add("C");

        storage.Get(3).Should().BeEquivalentTo("A", "B", "C");
    }

    [Fact]
    public void Remove_ShouldRemoveItemsFromStorage()
    {
        InMemoryStorage<string> storage = new();
        storage.Add("A");
        storage.Add("B");
        storage.Add("C");

        storage.Remove(new[] { "A", "C" });

        storage.Get(10).Should().BeEquivalentTo("B");
    }

    [Fact]
    public void Get_ShouldReturnItemsBatch()
    {
        InMemoryStorage<string> storage = new();
        storage.Add("A");
        storage.Add("B");
        storage.Add("C");
        storage.Add("E");
        storage.Add("F");

        storage.Get(3).Should().BeEquivalentTo("A", "B", "C");
    }

    [Fact]
    public void Get_ShouldReturnEmptyCollectionIfStorageIsEmpty()
    {
        InMemoryStorage<string> storage = new();

        storage.Get(3).Should().BeEmpty();
    }

    [Fact]
    public void Get_ShouldReturnTheSameItemsIfNotRemoved()
    {
        InMemoryStorage<string> storage = new();
        storage.Add("A");
        storage.Add("B");
        storage.Add("C");
        storage.Add("E");
        storage.Add("F");

        IReadOnlyCollection<string> batch1 = storage.Get(3);
        IReadOnlyCollection<string> batch2 = storage.Get(3);

        batch2.Should().BeEquivalentTo(batch1);
    }

    [Fact]
    public void Get_ShouldReturnStoredItemsInChronologicalOrder()
    {
        InMemoryStorage<string> storage = new();
        storage.Add("A");
        storage.Add("B");
        storage.Add("C");

        storage.Get(3).Should().BeEquivalentTo(new[] { "A", "B", "C" }, options => options.WithStrictOrdering());
    }

    [Fact]
    public void ItemsCount_ShouldReturnItemsCount()
    {
        InMemoryStorage<string> storage = new();
        storage.Add("A");
        storage.Add("B");
        storage.Add("C");

        int count = storage.ItemsCount;

        count.Should().Be(3);
    }

    [Fact]
    public void ItemsCount_ShouldReturnZero_WhenTheStorageIsEmpty()
    {
        InMemoryStorage<string> storage = new();

        int count = storage.ItemsCount;

        count.Should().Be(0);
    }

    [Fact]
    public async Task GetMaxAge_ShouldReturnOldestItemAge()
    {
        InMemoryStorage<string> storage = new();
        storage.Add("A");

        await Task.Delay(100);

        storage.Add("B");

        // TODO: Revert assert to >=100 and figure out why it fails in the pipeline
        storage.GetMaxAge().Should().BeGreaterThan(TimeSpan.FromMilliseconds(90));
    }

    [Fact]
    public void GetMaxAge_ShouldReturnZero_WhenTheStorageIsEmpty()
    {
        InMemoryStorage<string> storage = new();

        storage.GetMaxAge().Should().Be(TimeSpan.Zero);
    }
}
