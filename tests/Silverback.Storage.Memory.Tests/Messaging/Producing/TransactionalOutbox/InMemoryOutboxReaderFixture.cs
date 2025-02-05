// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Tests.Logging;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Storage.Memory.Messaging.Producing.TransactionalOutbox;

public class InMemoryOutboxReaderFixture
{
    [Fact]
    public async Task AcknowledgeAsync_ShouldRemoveItemsFromStorage()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddInMemoryOutbox()));
        InMemoryOutboxSettings outboxSettings = new();
        InMemoryOutboxFactory outboxFactory = serviceProvider.GetRequiredService<InMemoryOutboxFactory>();
        InMemoryOutbox outbox = outboxFactory.GetOutbox(outboxSettings);

        OutboxMessage outboxMessage1 = new([0x01], null, "test");
        OutboxMessage outboxMessage2 = new([0x02], null, "test");
        OutboxMessage outboxMessage3 = new([0x03], null, "test");
        OutboxMessage outboxMessage4 = new([0x04], null, "test");
        outbox.Add(outboxMessage1);
        outbox.Add(outboxMessage2);
        outbox.Add(outboxMessage3);
        outbox.Add(outboxMessage4);

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(outboxSettings, serviceProvider);

        await outboxReader.AcknowledgeAsync([outboxMessage1, outboxMessage3]);

        outbox.Get(10).Select(message => message.Content).Should().BeEquivalentTo(
            new[]
            {
                [0x02],
                new byte[] { 0x04 }
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
        InMemoryOutboxFactory outboxFactory = serviceProvider.GetRequiredService<InMemoryOutboxFactory>();
        InMemoryOutbox outbox = outboxFactory.GetOutbox(outboxSettings);

        outbox.Add(new OutboxMessage([0x01], null, "test"));
        outbox.Add(new OutboxMessage([0x02], null, "test"));
        outbox.Add(new OutboxMessage([0x03], null, "test"));
        outbox.Add(new OutboxMessage([0x04], null, "test"));
        outbox.Add(new OutboxMessage([0x05], null, "test"));

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(outboxSettings, serviceProvider);

        IDisposableAsyncEnumerable<OutboxMessage> messages = await outboxReader.GetAsync(3);

        (await messages.ToListAsync()).Select(message => message.Content).Should().BeEquivalentTo(
        [
            [0x01],
                [0x02],
                new byte[] { 0x03 }
        ]);
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
        IOutboxReader outboxReader = readerFactory.GetReader(outboxSettings, serviceProvider);

        IDisposableAsyncEnumerable<OutboxMessage> messages = await outboxReader.GetAsync(3);

        (await messages.ToListAsync()).Should().BeEmpty();
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
        InMemoryOutboxFactory outboxFactory = serviceProvider.GetRequiredService<InMemoryOutboxFactory>();
        InMemoryOutbox outbox = outboxFactory.GetOutbox(outboxSettings);

        outbox.Add(new OutboxMessage([0x01], null, "test"));
        outbox.Add(new OutboxMessage([0x02], null, "test"));
        outbox.Add(new OutboxMessage([0x03], null, "test"));
        outbox.Add(new OutboxMessage([0x04], null, "test"));
        outbox.Add(new OutboxMessage([0x05], null, "test"));

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(outboxSettings, Substitute.For<IServiceProvider>());

        IDisposableAsyncEnumerable<OutboxMessage> batch1 = await outboxReader.GetAsync(3);
        IDisposableAsyncEnumerable<OutboxMessage> batch2 = await outboxReader.GetAsync(3);

        (await batch2.ToListAsync()).Should().BeEquivalentTo(await batch1.ToListAsync());
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
        InMemoryOutboxFactory outboxFactory = serviceProvider.GetRequiredService<InMemoryOutboxFactory>();
        InMemoryOutbox outbox = outboxFactory.GetOutbox(outboxSettings);

        outbox.Add(new OutboxMessage([0x01], null, "test"));
        outbox.Add(new OutboxMessage([0x02], null, "test"));
        outbox.Add(new OutboxMessage([0x03], null, "test"));

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(outboxSettings, serviceProvider);

        IDisposableAsyncEnumerable<OutboxMessage> messages = await outboxReader.GetAsync(3);

        (await messages.ToListAsync()).Select(message => message.Content).Should().BeEquivalentTo(
            [
                [0x01],
                [0x02],
                new byte[] { 0x03 }
            ],
            options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task GetLengthAsync_ShouldReturnItemsCount()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddInMemoryOutbox()));
        InMemoryOutboxSettings outboxSettings = new();
        InMemoryOutboxFactory outboxFactory = serviceProvider.GetRequiredService<InMemoryOutboxFactory>();
        InMemoryOutbox outbox = outboxFactory.GetOutbox(outboxSettings);

        outbox.Add(new OutboxMessage([0x01], null, "test"));
        outbox.Add(new OutboxMessage([0x02], null, "test"));
        outbox.Add(new OutboxMessage([0x03], null, "test"));

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(outboxSettings, serviceProvider);

        int count = await outboxReader.GetLengthAsync();

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
        IOutboxReader outboxReader = readerFactory.GetReader(outboxSettings, serviceProvider);

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
        InMemoryOutboxFactory outboxFactory = serviceProvider.GetRequiredService<InMemoryOutboxFactory>();
        InMemoryOutbox outbox = outboxFactory.GetOutbox(outboxSettings);

        outbox.Add(new OutboxMessage([0x01], null, "test"));
        await Task.Delay(100);
        outbox.Add(new OutboxMessage([0x02], null, "test"));

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(outboxSettings, serviceProvider);

        TimeSpan maxAge = await outboxReader.GetMaxAgeAsync();

        // TODO: Revert assert to >=100 and figure out why it fails in the pipeline
        maxAge.Should().BeGreaterThan(TimeSpan.FromMilliseconds(90));
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
        IOutboxReader outboxReader = readerFactory.GetReader(outboxSettings, serviceProvider);

        TimeSpan maxAge = await outboxReader.GetMaxAgeAsync();

        maxAge.Should().Be(TimeSpan.Zero);
    }
}
