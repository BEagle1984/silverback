// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Outbound.TransactionalOutbox;
using Silverback.Storage;
using Silverback.Storage.DataAccess;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Storage.Sqlite.Messaging.Outbound.TransactionalOutbox;

public sealed class SqliteOutboxReaderFixture : IDisposable
{
    private static readonly OutboxMessageEndpoint Endpoint = new("test", null, null);

    private readonly SqliteOutboxSettings _outboxSettings;

    private readonly SqliteConnection _sqliteConnection;

    private readonly SqliteDataAccess _dataAccess;

    private readonly SqliteOutboxWriter _outboxWriter;

    public SqliteOutboxReaderFixture()
    {
        _outboxSettings = new SqliteOutboxSettings($"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared", "TestOutbox");
        _sqliteConnection = new SqliteConnection(_outboxSettings.ConnectionString);
        _sqliteConnection.Open();

        _dataAccess = new SqliteDataAccess(_sqliteConnection.ConnectionString);
        _outboxWriter = new SqliteOutboxWriter(_outboxSettings);
    }

    [Fact]
    public async Task AcknowledgeAsync_ShouldRemoveItemsFromStorage()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddSqliteOutbox()));

        SilverbackStorageInitializer storageInitializer = serviceProvider.GetRequiredService<SilverbackStorageInitializer>();
        await storageInitializer.CreateSqliteOutboxAsync(_outboxSettings);

        DbOutboxMessage outboxMessage1 = new(1, typeof(TestMessage), new byte[] { 0x01 }, null, Endpoint);
        DbOutboxMessage outboxMessage2 = new(2, typeof(TestMessage), new byte[] { 0x02 }, null, Endpoint);
        DbOutboxMessage outboxMessage3 = new(3, typeof(TestMessage), new byte[] { 0x03 }, null, Endpoint);
        DbOutboxMessage outboxMessage4 = new(4, typeof(TestMessage), new byte[] { 0x04 }, null, Endpoint);
        await _outboxWriter.AddAsync(outboxMessage1);
        await _outboxWriter.AddAsync(outboxMessage2);
        await _outboxWriter.AddAsync(outboxMessage3);
        await _outboxWriter.AddAsync(outboxMessage4);

        (await _dataAccess.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM TestOutbox")).Should().Be(4);

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(_outboxSettings);

        await outboxReader.AcknowledgeAsync(new[] { outboxMessage1, outboxMessage3 });

        (await _dataAccess.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM TestOutbox")).Should().Be(2);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnItemsBatch()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddSqliteOutbox()));

        SilverbackStorageInitializer storageInitializer = serviceProvider.GetRequiredService<SilverbackStorageInitializer>();
        await storageInitializer.CreateSqliteOutboxAsync(_outboxSettings);

        await _outboxWriter.AddAsync(new OutboxMessage(typeof(TestMessage), new byte[] { 0x01 }, null, Endpoint));
        await _outboxWriter.AddAsync(new OutboxMessage(typeof(TestMessage), new byte[] { 0x02 }, null, Endpoint));
        await _outboxWriter.AddAsync(new OutboxMessage(typeof(TestMessage), new byte[] { 0x03 }, null, Endpoint));
        await _outboxWriter.AddAsync(new OutboxMessage(typeof(TestMessage), new byte[] { 0x04 }, null, Endpoint));
        await _outboxWriter.AddAsync(new OutboxMessage(typeof(TestMessage), new byte[] { 0x05 }, null, Endpoint));

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(_outboxSettings);

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
                .WithConnectionToMessageBroker(options => options.AddSqliteOutbox()));

        SilverbackStorageInitializer storageInitializer = serviceProvider.GetRequiredService<SilverbackStorageInitializer>();
        await storageInitializer.CreateSqliteOutboxAsync(_outboxSettings);

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(_outboxSettings);

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
                .WithConnectionToMessageBroker(options => options.AddSqliteOutbox()));

        SilverbackStorageInitializer storageInitializer = serviceProvider.GetRequiredService<SilverbackStorageInitializer>();
        await storageInitializer.CreateSqliteOutboxAsync(_outboxSettings);

        await _outboxWriter.AddAsync(new OutboxMessage(typeof(TestMessage), new byte[] { 0x01 }, null, Endpoint));
        await _outboxWriter.AddAsync(new OutboxMessage(typeof(TestMessage), new byte[] { 0x02 }, null, Endpoint));
        await _outboxWriter.AddAsync(new OutboxMessage(typeof(TestMessage), new byte[] { 0x03 }, null, Endpoint));
        await _outboxWriter.AddAsync(new OutboxMessage(typeof(TestMessage), new byte[] { 0x04 }, null, Endpoint));
        await _outboxWriter.AddAsync(new OutboxMessage(typeof(TestMessage), new byte[] { 0x05 }, null, Endpoint));

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(_outboxSettings);

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
                .WithConnectionToMessageBroker(options => options.AddSqliteOutbox()));

        SilverbackStorageInitializer storageInitializer = serviceProvider.GetRequiredService<SilverbackStorageInitializer>();
        await storageInitializer.CreateSqliteOutboxAsync(_outboxSettings);

        await _outboxWriter.AddAsync(new OutboxMessage(typeof(TestMessage), new byte[] { 0x01 }, null, Endpoint));
        await _outboxWriter.AddAsync(new OutboxMessage(typeof(TestMessage), new byte[] { 0x02 }, null, Endpoint));
        await _outboxWriter.AddAsync(new OutboxMessage(typeof(TestMessage), new byte[] { 0x03 }, null, Endpoint));

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(_outboxSettings);

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
                .WithConnectionToMessageBroker(options => options.AddSqliteOutbox()));

        SilverbackStorageInitializer storageInitializer = serviceProvider.GetRequiredService<SilverbackStorageInitializer>();
        await storageInitializer.CreateSqliteOutboxAsync(_outboxSettings);

        await _outboxWriter.AddAsync(new OutboxMessage(typeof(TestMessage), new byte[] { 0x01 }, null, Endpoint));
        await _outboxWriter.AddAsync(new OutboxMessage(typeof(TestMessage), new byte[] { 0x02 }, null, Endpoint));
        await _outboxWriter.AddAsync(new OutboxMessage(typeof(TestMessage), new byte[] { 0x03 }, null, Endpoint));

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(_outboxSettings);

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
                .WithConnectionToMessageBroker(options => options.AddSqliteOutbox()));

        SilverbackStorageInitializer storageInitializer = serviceProvider.GetRequiredService<SilverbackStorageInitializer>();
        await storageInitializer.CreateSqliteOutboxAsync(_outboxSettings);

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(_outboxSettings);

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
                .WithConnectionToMessageBroker(options => options.AddSqliteOutbox()));

        SilverbackStorageInitializer storageInitializer = serviceProvider.GetRequiredService<SilverbackStorageInitializer>();
        await storageInitializer.CreateSqliteOutboxAsync(_outboxSettings);

        await _outboxWriter.AddAsync(new OutboxMessage(typeof(TestMessage), new byte[] { 0x01 }, null, Endpoint));
        await Task.Delay(100);
        await _outboxWriter.AddAsync(new OutboxMessage(typeof(TestMessage), new byte[] { 0x02 }, null, Endpoint));

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(_outboxSettings);

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
                .WithConnectionToMessageBroker(options => options.AddSqliteOutbox()));

        SilverbackStorageInitializer storageInitializer = serviceProvider.GetRequiredService<SilverbackStorageInitializer>();
        await storageInitializer.CreateSqliteOutboxAsync(_outboxSettings);

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(_outboxSettings);

        TimeSpan maxAge = await outboxReader.GetMaxAgeAsync();

        maxAge.Should().Be(TimeSpan.Zero);
    }

    public void Dispose() => _sqliteConnection.Dispose();

    [SuppressMessage("", "CA1812", Justification = "Class used for testing")]
    private class TestMessage
    {
    }
}
