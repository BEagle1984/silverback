// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Storage;
using Silverback.Storage.DataAccess;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Storage.PostgreSql.Messaging.Producing.TransactionalOutbox;

public sealed class PostgreSqlOutboxReaderFixture : PostgresContainerFixture
{
    private static readonly OutboxMessageEndpoint Endpoint = new("test", null);

    private readonly PostgreSqlOutboxSettings _outboxSettings;

    private readonly PostgreSqlDataAccess _dataAccess;

    private readonly PostgreSqlOutboxWriter _outboxWriter;

    public PostgreSqlOutboxReaderFixture()
    {
        _outboxSettings = new PostgreSqlOutboxSettings(ConnectionString);
        _dataAccess = new PostgreSqlDataAccess(ConnectionString);
        _outboxWriter = new PostgreSqlOutboxWriter(_outboxSettings);
    }

    [Fact]
    public async Task AcknowledgeAsync_ShouldRemoveItemsFromStorage()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddPostgreSqlOutbox()));

        SilverbackStorageInitializer storageInitializer = serviceProvider.GetRequiredService<SilverbackStorageInitializer>();
        await storageInitializer.CreatePostgreSqlOutboxAsync(_outboxSettings);

        DbOutboxMessage outboxMessage1 = new(1, [0x01], null, Endpoint);
        DbOutboxMessage outboxMessage2 = new(2, [0x02], null, Endpoint);
        DbOutboxMessage outboxMessage3 = new(3, [0x03], null, Endpoint);
        DbOutboxMessage outboxMessage4 = new(4, [0x04], null, Endpoint);
        await _outboxWriter.AddAsync(outboxMessage1);
        await _outboxWriter.AddAsync(outboxMessage2);
        await _outboxWriter.AddAsync(outboxMessage3);
        await _outboxWriter.AddAsync(outboxMessage4);

        (await GetOutboxLengthAsync()).Should().Be(4);

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(_outboxSettings, serviceProvider);

        await outboxReader.AcknowledgeAsync([outboxMessage1, outboxMessage3]);

        (await GetOutboxLengthAsync()).Should().Be(2);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnItemsBatch()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddPostgreSqlOutbox()));

        SilverbackStorageInitializer storageInitializer = serviceProvider.GetRequiredService<SilverbackStorageInitializer>();
        await storageInitializer.CreatePostgreSqlOutboxAsync(_outboxSettings);

        await _outboxWriter.AddAsync(new OutboxMessage([0x01], null, Endpoint));
        await _outboxWriter.AddAsync(new OutboxMessage([0x02], null, Endpoint));
        await _outboxWriter.AddAsync(new OutboxMessage([0x03], null, Endpoint));
        await _outboxWriter.AddAsync(new OutboxMessage([0x04], null, Endpoint));
        await _outboxWriter.AddAsync(new OutboxMessage([0x05], null, Endpoint));

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(_outboxSettings, serviceProvider);

        IReadOnlyCollection<OutboxMessage> messages = await outboxReader.GetAsync(3);

        messages.Select(message => message.Content).Should().BeEquivalentTo(
            new[]
            {
                [0x01],
                [0x02],
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
                .WithConnectionToMessageBroker(options => options.AddPostgreSqlOutbox()));

        SilverbackStorageInitializer storageInitializer = serviceProvider.GetRequiredService<SilverbackStorageInitializer>();
        await storageInitializer.CreatePostgreSqlOutboxAsync(_outboxSettings);

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(_outboxSettings, serviceProvider);

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
                .WithConnectionToMessageBroker(options => options.AddPostgreSqlOutbox()));

        SilverbackStorageInitializer storageInitializer = serviceProvider.GetRequiredService<SilverbackStorageInitializer>();
        await storageInitializer.CreatePostgreSqlOutboxAsync(_outboxSettings);

        await _outboxWriter.AddAsync(new OutboxMessage([0x01], null, Endpoint));
        await _outboxWriter.AddAsync(new OutboxMessage([0x02], null, Endpoint));
        await _outboxWriter.AddAsync(new OutboxMessage([0x03], null, Endpoint));
        await _outboxWriter.AddAsync(new OutboxMessage([0x04], null, Endpoint));
        await _outboxWriter.AddAsync(new OutboxMessage([0x05], null, Endpoint));

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(_outboxSettings, serviceProvider);

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
                .WithConnectionToMessageBroker(options => options.AddPostgreSqlOutbox()));

        SilverbackStorageInitializer storageInitializer = serviceProvider.GetRequiredService<SilverbackStorageInitializer>();
        await storageInitializer.CreatePostgreSqlOutboxAsync(_outboxSettings);

        await _outboxWriter.AddAsync(new OutboxMessage([0x01], null, Endpoint));
        await _outboxWriter.AddAsync(new OutboxMessage([0x02], null, Endpoint));
        await _outboxWriter.AddAsync(new OutboxMessage([0x03], null, Endpoint));

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(_outboxSettings, serviceProvider);

        IReadOnlyCollection<OutboxMessage> messages = await outboxReader.GetAsync(3);

        messages.Select(message => message.Content).Should().BeEquivalentTo(
            new[]
            {
                [0x01],
                [0x02],
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
                .WithConnectionToMessageBroker(options => options.AddPostgreSqlOutbox()));

        SilverbackStorageInitializer storageInitializer = serviceProvider.GetRequiredService<SilverbackStorageInitializer>();
        await storageInitializer.CreatePostgreSqlOutboxAsync(_outboxSettings);

        await _outboxWriter.AddAsync(new OutboxMessage([0x01], null, Endpoint));
        await _outboxWriter.AddAsync(new OutboxMessage([0x02], null, Endpoint));
        await _outboxWriter.AddAsync(new OutboxMessage([0x03], null, Endpoint));

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(_outboxSettings, serviceProvider);

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
                .WithConnectionToMessageBroker(options => options.AddPostgreSqlOutbox()));

        SilverbackStorageInitializer storageInitializer = serviceProvider.GetRequiredService<SilverbackStorageInitializer>();
        await storageInitializer.CreatePostgreSqlOutboxAsync(_outboxSettings);

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(_outboxSettings, serviceProvider);

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
                .WithConnectionToMessageBroker(options => options.AddPostgreSqlOutbox()));

        SilverbackStorageInitializer storageInitializer = serviceProvider.GetRequiredService<SilverbackStorageInitializer>();
        await storageInitializer.CreatePostgreSqlOutboxAsync(_outboxSettings);

        await _outboxWriter.AddAsync(new OutboxMessage([0x01], null, Endpoint));
        await Task.Delay(100);
        await _outboxWriter.AddAsync(new OutboxMessage([0x02], null, Endpoint));

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(_outboxSettings, serviceProvider);

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
                .WithConnectionToMessageBroker(options => options.AddPostgreSqlOutbox()));

        SilverbackStorageInitializer storageInitializer = serviceProvider.GetRequiredService<SilverbackStorageInitializer>();
        await storageInitializer.CreatePostgreSqlOutboxAsync(_outboxSettings);

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(_outboxSettings, serviceProvider);

        TimeSpan maxAge = await outboxReader.GetMaxAgeAsync();

        maxAge.Should().Be(TimeSpan.Zero);
    }

    private Task<long> GetOutboxLengthAsync() =>
        _dataAccess.ExecuteScalarAsync<long>(
            $"SELECT COUNT(*) FROM \"{_outboxSettings.TableName}\"",
            null,
            TimeSpan.FromSeconds(1));
}