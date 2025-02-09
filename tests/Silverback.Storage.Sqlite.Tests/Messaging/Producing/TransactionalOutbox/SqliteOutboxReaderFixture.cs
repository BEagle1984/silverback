// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Storage;
using Silverback.Storage.DataAccess;
using Silverback.Tests.Logging;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Storage.Sqlite.Messaging.Producing.TransactionalOutbox;

public sealed class SqliteOutboxReaderFixture : IDisposable
{
    private readonly SqliteOutboxSettings _outboxSettings;

    private readonly SqliteConnection _sqliteConnection;

    private readonly SqliteDataAccess _dataAccess;

    private readonly SqliteOutboxWriter _outboxWriter;

    public SqliteOutboxReaderFixture()
    {
        _outboxSettings = new SqliteOutboxSettings($"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        _sqliteConnection = new SqliteConnection(_outboxSettings.ConnectionString);
        _sqliteConnection.Open();

        _dataAccess = new SqliteDataAccess(_outboxSettings.ConnectionString);
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

        DbOutboxMessage outboxMessage1 = new(1, [0x01], null, "test");
        DbOutboxMessage outboxMessage2 = new(2, [0x02], null, "test");
        DbOutboxMessage outboxMessage3 = new(3, [0x03], null, "test");
        DbOutboxMessage outboxMessage4 = new(4, [0x04], null, "test");
        await _outboxWriter.AddAsync(outboxMessage1);
        await _outboxWriter.AddAsync(outboxMessage2);
        await _outboxWriter.AddAsync(outboxMessage3);
        await _outboxWriter.AddAsync(outboxMessage4);

        (await GetOutboxLengthAsync()).ShouldBe(4);

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(_outboxSettings, serviceProvider);

        await outboxReader.AcknowledgeAsync([outboxMessage1, outboxMessage3]);

        (await GetOutboxLengthAsync()).ShouldBe(2);
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

        await _outboxWriter.AddAsync(new OutboxMessage([0x01], null, "test"));
        await _outboxWriter.AddAsync(new OutboxMessage([0x02], null, "test"));
        await _outboxWriter.AddAsync(new OutboxMessage([0x03], null, "test"));
        await _outboxWriter.AddAsync(new OutboxMessage([0x04], null, "test"));
        await _outboxWriter.AddAsync(new OutboxMessage([0x05], null, "test"));

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(_outboxSettings, serviceProvider);

        IDisposableAsyncEnumerable<OutboxMessage> messages = await outboxReader.GetAsync(3);

        (await messages.ToListAsync()).Select(message => message.Content).ShouldBe(
        [
            [0x01],
            [0x02],
            [0x03]
        ]);
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
        IOutboxReader outboxReader = readerFactory.GetReader(_outboxSettings, serviceProvider);

        IDisposableAsyncEnumerable<OutboxMessage> messages = await outboxReader.GetAsync(3);

        (await messages.ToListAsync()).ShouldBeEmpty();
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

        await _outboxWriter.AddAsync(new OutboxMessage([0x01], null, "test"));
        await _outboxWriter.AddAsync(new OutboxMessage([0x02], null, "test"));
        await _outboxWriter.AddAsync(new OutboxMessage([0x03], null, "test"));
        await _outboxWriter.AddAsync(new OutboxMessage([0x04], null, "test"));
        await _outboxWriter.AddAsync(new OutboxMessage([0x05], null, "test"));

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(_outboxSettings, serviceProvider);

        IDisposableAsyncEnumerable<OutboxMessage> batch1 = await outboxReader.GetAsync(3);
        IDisposableAsyncEnumerable<OutboxMessage> batch2 = await outboxReader.GetAsync(3);

        List<OutboxMessage> batch1Messages = await batch1.ToListAsync();
        List<OutboxMessage> batch2Messages = await batch2.ToListAsync();
        batch2Messages.ShouldBeEquivalentTo(batch1Messages);
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

        await _outboxWriter.AddAsync(new OutboxMessage([0x01], null, "test"));
        await _outboxWriter.AddAsync(new OutboxMessage([0x02], null, "test"));
        await _outboxWriter.AddAsync(new OutboxMessage([0x03], null, "test"));

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(_outboxSettings, serviceProvider);

        IDisposableAsyncEnumerable<OutboxMessage> messages = await outboxReader.GetAsync(3);

        (await messages.ToListAsync()).Select(message => message.Content).ShouldBe(
            [
                [0x01],
                [0x02],
                [0x03]
            ]);
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

        await _outboxWriter.AddAsync(new OutboxMessage([0x01], null, "test"));
        await _outboxWriter.AddAsync(new OutboxMessage([0x02], null, "test"));
        await _outboxWriter.AddAsync(new OutboxMessage([0x03], null, "test"));

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(_outboxSettings, serviceProvider);

        int count = await outboxReader.GetLengthAsync();

        count.ShouldBe(3);
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
        IOutboxReader outboxReader = readerFactory.GetReader(_outboxSettings, serviceProvider);

        int count = await outboxReader.GetLengthAsync();

        count.ShouldBe(0);
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

        await _outboxWriter.AddAsync(new OutboxMessage([0x01], null, "test"));
        await Task.Delay(100);
        await _outboxWriter.AddAsync(new OutboxMessage([0x02], null, "test"));

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(_outboxSettings, serviceProvider);

        TimeSpan maxAge = await outboxReader.GetMaxAgeAsync();

        maxAge.ShouldBeGreaterThan(TimeSpan.FromMilliseconds(90)); // Exact value causes flaky tests on CI pipeline
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
        IOutboxReader outboxReader = readerFactory.GetReader(_outboxSettings, serviceProvider);

        TimeSpan maxAge = await outboxReader.GetMaxAgeAsync();

        maxAge.ShouldBe(TimeSpan.Zero);
    }

    public void Dispose() => _sqliteConnection.Dispose();

    private Task<long> GetOutboxLengthAsync() =>
        _dataAccess.ExecuteScalarAsync<long>(
            $"SELECT COUNT(*) FROM {_outboxSettings.TableName}",
            null,
            TimeSpan.FromSeconds(1));
}
