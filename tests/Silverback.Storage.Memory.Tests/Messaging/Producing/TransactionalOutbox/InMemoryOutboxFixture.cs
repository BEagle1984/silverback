// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Xunit;

namespace Silverback.Tests.Storage.Memory.Messaging.Producing.TransactionalOutbox;

public class InMemoryOutboxFixture
{
    [Fact]
    public void Add_ShouldAddItemToOutbox()
    {
        OutboxMessage outboxMessage1 = new(null, null, "1");
        OutboxMessage outboxMessage2 = new(null, null, "2");
        OutboxMessage outboxMessage3 = new(null, null, "3");
        InMemoryOutbox outbox = new();
        outbox.Add(outboxMessage1);
        outbox.Add(outboxMessage2);
        outbox.Add(outboxMessage3);

        outbox.Get(3).Should().BeEquivalentTo(new[] { outboxMessage1, outboxMessage2, outboxMessage3 });
    }

    [Fact]
    public void Remove_ShouldRemoveItemsFromOutbox()
    {
        OutboxMessage outboxMessage1 = new(null, null, "1");
        OutboxMessage outboxMessage2 = new(null, null, "2");
        OutboxMessage outboxMessage3 = new(null, null, "3");
        InMemoryOutbox outbox = new();
        outbox.Add(outboxMessage1);
        outbox.Add(outboxMessage2);
        outbox.Add(outboxMessage3);

        outbox.Remove([outboxMessage1, outboxMessage2]);

        outbox.Get(10).Should().BeEquivalentTo(new[] { outboxMessage3 });
    }

    [Fact]
    public void Get_ShouldReturnMessagesBatchInChronologicalOrder()
    {
        OutboxMessage outboxMessage1 = new(null, null, "1");
        OutboxMessage outboxMessage2 = new(null, null, "2");
        OutboxMessage outboxMessage3 = new(null, null, "3");
        InMemoryOutbox outbox = new();
        outbox.Add(outboxMessage1);
        outbox.Add(outboxMessage2);
        outbox.Add(outboxMessage3);

        outbox.Get(2).Should().BeEquivalentTo(new[] { outboxMessage1, outboxMessage2 }, options => options.WithStrictOrdering());
    }

    [Fact]
    public void Get_ShouldReturnEmptyCollectionIfOutboxIsEmpty()
    {
        InMemoryOutbox outbox = new();

        outbox.Get(3).Should().BeEmpty();
    }

    [Fact]
    public void Get_ShouldReturnTheSameItemsIfNotRemoved()
    {
        OutboxMessage outboxMessage1 = new(null, null, "1");
        OutboxMessage outboxMessage2 = new(null, null, "2");
        OutboxMessage outboxMessage3 = new(null, null, "3");
        InMemoryOutbox outbox = new();
        outbox.Add(outboxMessage1);
        outbox.Add(outboxMessage2);
        outbox.Add(outboxMessage3);

        IReadOnlyCollection<OutboxMessage> batch1 = outbox.Get(3);
        IReadOnlyCollection<OutboxMessage> batch2 = outbox.Get(3);

        batch2.Should().BeEquivalentTo(batch1);
    }

    [Fact]
    public void ItemsCount_ShouldReturnItemsCount()
    {
        OutboxMessage outboxMessage1 = new(null, null, "1");
        OutboxMessage outboxMessage2 = new(null, null, "2");
        OutboxMessage outboxMessage3 = new(null, null, "3");
        InMemoryOutbox outbox = new();
        outbox.Add(outboxMessage1);
        outbox.Add(outboxMessage2);
        outbox.Add(outboxMessage3);

        int count = outbox.ItemsCount;

        count.Should().Be(3);
    }

    [Fact]
    public void ItemsCount_ShouldReturnZero_WhenTheOutboxIsEmpty()
    {
        InMemoryOutbox outbox = new();

        int count = outbox.ItemsCount;

        count.Should().Be(0);
    }

    [Fact]
    public async Task GetMaxAge_ShouldReturnOldestItemAge()
    {
        InMemoryOutbox outbox = new();
        outbox.Add(new OutboxMessage(null, null, "1"));

        await Task.Delay(100);

        outbox.Add(new OutboxMessage(null, null, "2"));

        outbox.GetMaxAge().Should().BeGreaterThan(TimeSpan.FromMilliseconds(90));
    }

    [Fact]
    public void GetMaxAge_ShouldReturnZero_WhenTheOutboxIsEmpty()
    {
        InMemoryOutbox outbox = new();

        outbox.GetMaxAge().Should().Be(TimeSpan.Zero);
    }
}
