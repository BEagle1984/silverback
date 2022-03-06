// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Collections;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Outbound.TransactionalOutbox;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Storage.Memory.Messaging.Outbound.TransactionalOutbox;

public class InMemoryOutboxReaderFixture
{
    private static readonly OutboxMessageEndpoint Endpoint = new("test", null, null);

    [Fact]
    public async Task AcknowledgeAsync_ShouldRemoveItemsFromStorage()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddInMemoryOutbox()));
        InMemoryOutboxSettings outboxSettings = new();
        InMemoryStorageFactory storageFactory = serviceProvider.GetRequiredService<InMemoryStorageFactory>();
        InMemoryStorage<OutboxMessage> storage = storageFactory.GetStorage<OutboxSettings, OutboxMessage>(outboxSettings);

        OutboxMessage outboxMessage1 = new(typeof(TestMessage), new byte[] { 0x01 }, null, Endpoint);
        OutboxMessage outboxMessage2 = new(typeof(TestMessage), new byte[] { 0x02 }, null, Endpoint);
        OutboxMessage outboxMessage3 = new(typeof(TestMessage), new byte[] { 0x03 }, null, Endpoint);
        OutboxMessage outboxMessage4 = new(typeof(TestMessage), new byte[] { 0x04 }, null, Endpoint);
        storage.Add(outboxMessage1);
        storage.Add(outboxMessage2);
        storage.Add(outboxMessage3);
        storage.Add(outboxMessage4);

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(outboxSettings);

        await outboxReader.AcknowledgeAsync(new[] { outboxMessage1, outboxMessage3 });

        storage.Get(10).Select(message => message.Content).Should().BeEquivalentTo(
            new[]
            {
                new byte[] { 0x02 },
                new byte[] { 0x04 },
            });
    }

    [Fact]
    public async Task GetAsync_ShouldReturnItemsBatch()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddInMemoryOutbox()));
        InMemoryOutboxSettings outboxSettings = new();
        InMemoryStorageFactory storageFactory = serviceProvider.GetRequiredService<InMemoryStorageFactory>();
        InMemoryStorage<OutboxMessage> storage = storageFactory.GetStorage<OutboxSettings, OutboxMessage>(outboxSettings);

        storage.Add(new OutboxMessage(typeof(TestMessage), new byte[] { 0x01 }, null, Endpoint));
        storage.Add(new OutboxMessage(typeof(TestMessage), new byte[] { 0x02 }, null, Endpoint));
        storage.Add(new OutboxMessage(typeof(TestMessage), new byte[] { 0x03 }, null, Endpoint));
        storage.Add(new OutboxMessage(typeof(TestMessage), new byte[] { 0x04 }, null, Endpoint));
        storage.Add(new OutboxMessage(typeof(TestMessage), new byte[] { 0x05 }, null, Endpoint));

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(outboxSettings);

        IReadOnlyCollection<OutboxMessage> messages = await outboxReader.GetAsync(3);

        messages.Select(message => message.Content).Should().BeEquivalentTo(
            new[]
            {
                new byte[] { 0x01 },
                new byte[] { 0x02 },
                new byte[] { 0x03 }
            });
    }

    [Fact]
    public async Task GetAsync_ShouldReturnEmptyCollectionIfStorageIsEmpty()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddInMemoryOutbox()));

        InMemoryOutboxSettings outboxSettings = new();
        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(outboxSettings);

        IReadOnlyCollection<OutboxMessage> messages = await outboxReader.GetAsync(3);

        messages.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAsync_ShouldReturnTheSameItemsIfNotRemoved()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddInMemoryOutbox()));
        InMemoryOutboxSettings outboxSettings = new();
        InMemoryStorageFactory storageFactory = serviceProvider.GetRequiredService<InMemoryStorageFactory>();
        InMemoryStorage<OutboxMessage> storage = storageFactory.GetStorage<OutboxSettings, OutboxMessage>(outboxSettings);

        storage.Add(new OutboxMessage(typeof(TestMessage), new byte[] { 0x01 }, null, Endpoint));
        storage.Add(new OutboxMessage(typeof(TestMessage), new byte[] { 0x02 }, null, Endpoint));
        storage.Add(new OutboxMessage(typeof(TestMessage), new byte[] { 0x03 }, null, Endpoint));
        storage.Add(new OutboxMessage(typeof(TestMessage), new byte[] { 0x04 }, null, Endpoint));
        storage.Add(new OutboxMessage(typeof(TestMessage), new byte[] { 0x05 }, null, Endpoint));

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(outboxSettings);

        IReadOnlyCollection<OutboxMessage> batch1 = await outboxReader.GetAsync(3);
        IReadOnlyCollection<OutboxMessage> batch2 = await outboxReader.GetAsync(3);

        batch2.Should().BeEquivalentTo(batch1);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnStoredItemsInChronologicalOrder()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddInMemoryOutbox()));
        InMemoryOutboxSettings outboxSettings = new();
        InMemoryStorageFactory storageFactory = serviceProvider.GetRequiredService<InMemoryStorageFactory>();
        InMemoryStorage<OutboxMessage> storage = storageFactory.GetStorage<OutboxSettings, OutboxMessage>(outboxSettings);

        storage.Add(new OutboxMessage(typeof(TestMessage), new byte[] { 0x01 }, null, Endpoint));
        storage.Add(new OutboxMessage(typeof(TestMessage), new byte[] { 0x02 }, null, Endpoint));
        storage.Add(new OutboxMessage(typeof(TestMessage), new byte[] { 0x03 }, null, Endpoint));

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(outboxSettings);

        IReadOnlyCollection<OutboxMessage> messages = await outboxReader.GetAsync(3);

        messages.Select(message => message.Content).Should().BeEquivalentTo(
            new[]
            {
                new byte[] { 0x01 },
                new byte[] { 0x02 },
                new byte[] { 0x03 }
            },
            options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task GetLengthAsync_ShouldReturnCommittedItemsCount()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddInMemoryOutbox()));
        InMemoryOutboxSettings outboxSettings = new();
        InMemoryStorageFactory storageFactory = serviceProvider.GetRequiredService<InMemoryStorageFactory>();
        InMemoryStorage<OutboxMessage> storage = storageFactory.GetStorage<OutboxSettings, OutboxMessage>(outboxSettings);

        storage.Add(new OutboxMessage(typeof(TestMessage), new byte[] { 0x01 }, null, Endpoint));
        storage.Add(new OutboxMessage(typeof(TestMessage), new byte[] { 0x02 }, null, Endpoint));
        storage.Add(new OutboxMessage(typeof(TestMessage), new byte[] { 0x03 }, null, Endpoint));

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(outboxSettings);

        int count;
        using (TransactionScope transaction = new())
        {
            storage.Add(new OutboxMessage(typeof(TestMessage), new byte[] { 0x04 }, null, Endpoint));

            count = await outboxReader.GetLengthAsync();
        }

        count.Should().Be(3);
    }

    [Fact]
    public async Task GetLengthAsync_ShouldReturnZero_WhenTheStorageIsEmpty()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddInMemoryOutbox()));
        InMemoryOutboxSettings outboxSettings = new();
        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(outboxSettings);

        int count = await outboxReader.GetLengthAsync();

        count.Should().Be(0);
    }

    [Fact]
    public async Task GetMaxAgeAsync_ShouldReturnOldestItemAge()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddInMemoryOutbox()));
        InMemoryOutboxSettings outboxSettings = new();
        InMemoryStorageFactory storageFactory = serviceProvider.GetRequiredService<InMemoryStorageFactory>();
        InMemoryStorage<OutboxMessage> storage = storageFactory.GetStorage<OutboxSettings, OutboxMessage>(outboxSettings);

        storage.Add(new OutboxMessage(typeof(TestMessage), new byte[] { 0x01 }, null, Endpoint));
        await Task.Delay(100);
        storage.Add(new OutboxMessage(typeof(TestMessage), new byte[] { 0x02 }, null, Endpoint));

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(outboxSettings);

        TimeSpan maxAge = await outboxReader.GetMaxAgeAsync();

        maxAge.Should().BeGreaterThan(TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public async Task GetMaxAgeAsync_ShouldReturnZero_WhenTheStorageIsEmpty()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddInMemoryOutbox()));
        InMemoryOutboxSettings outboxSettings = new();
        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(outboxSettings);

        TimeSpan maxAge = await outboxReader.GetMaxAgeAsync();

        maxAge.Should().Be(TimeSpan.Zero);
    }

    private class TestMessage
    {
    }
}
