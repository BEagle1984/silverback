// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Storage;
using Silverback.Storage.Relational;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Storage.Sqlite.Messaging.Producing.TransactionalOutbox;

public sealed class SqliteOutboxWriterFixture : IDisposable
{
    private static readonly OutboxMessageEndpoint Endpoint = new("test", null);

    private readonly SqliteOutboxSettings _outboxSettings;

    private readonly SqliteConnection _sqliteConnection;

    private readonly SqliteOutboxReader _outboxReader;

    public SqliteOutboxWriterFixture()
    {
        _outboxSettings = new SqliteOutboxSettings($"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        _sqliteConnection = new SqliteConnection(_outboxSettings.ConnectionString);
        _sqliteConnection.Open();

        _outboxReader = new SqliteOutboxReader(_outboxSettings);
    }

    [Fact]
    public async Task AddAsync_ShouldAddItemToStorage()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddSqliteOutbox()));

        SilverbackStorageInitializer storageInitializer = serviceProvider.GetRequiredService<SilverbackStorageInitializer>();
        await storageInitializer.CreateSqliteOutboxAsync(_outboxSettings);

        IOutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<IOutboxWriterFactory>();
        IOutboxWriter outboxWriter = writerFactory.GetWriter(_outboxSettings, serviceProvider);

        OutboxMessage outboxMessage1 = new(new byte[] { 0x01 }, null, Endpoint);
        OutboxMessage outboxMessage2 = new(new byte[] { 0x02 }, null, Endpoint);
        OutboxMessage outboxMessage3 = new(new byte[] { 0x03 }, null, Endpoint);
        await outboxWriter.AddAsync(outboxMessage1);
        await outboxWriter.AddAsync(outboxMessage2);
        await outboxWriter.AddAsync(outboxMessage3);

        (await _outboxReader.GetAsync(10)).Should().BeEquivalentTo(new[] { outboxMessage1, outboxMessage2, outboxMessage3 });
    }

    [Fact]
    public async Task AddAsync_ShouldAddItemsToStorage()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddSqliteOutbox()));

        SilverbackStorageInitializer storageInitializer = serviceProvider.GetRequiredService<SilverbackStorageInitializer>();
        await storageInitializer.CreateSqliteOutboxAsync(_outboxSettings);

        IOutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<IOutboxWriterFactory>();
        IOutboxWriter outboxWriter = writerFactory.GetWriter(_outboxSettings, serviceProvider);

        OutboxMessage outboxMessage1 = new(new byte[] { 0x01 }, null, Endpoint);
        OutboxMessage outboxMessage2 = new(new byte[] { 0x02 }, null, Endpoint);
        OutboxMessage outboxMessage3 = new(new byte[] { 0x03 }, null, Endpoint);
        await outboxWriter.AddAsync(new[] { outboxMessage1, outboxMessage2, outboxMessage3 });

        (await _outboxReader.GetAsync(10)).Should().BeEquivalentTo(new[] { outboxMessage1, outboxMessage2, outboxMessage3 });
    }

    [Fact]
    public async Task AddAsync_ShouldAddAsyncItemsToStorage()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddSqliteOutbox()));

        SilverbackStorageInitializer storageInitializer = serviceProvider.GetRequiredService<SilverbackStorageInitializer>();
        await storageInitializer.CreateSqliteOutboxAsync(_outboxSettings);

        IOutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<IOutboxWriterFactory>();
        IOutboxWriter outboxWriter = writerFactory.GetWriter(_outboxSettings, serviceProvider);

        OutboxMessage outboxMessage1 = new(new byte[] { 0x01 }, null, Endpoint);
        OutboxMessage outboxMessage2 = new(new byte[] { 0x02 }, null, Endpoint);
        OutboxMessage outboxMessage3 = new(new byte[] { 0x03 }, null, Endpoint);
        await outboxWriter.AddAsync(new[] { outboxMessage1, outboxMessage2, outboxMessage3 }.ToAsyncEnumerable());

        (await _outboxReader.GetAsync(10)).Should().BeEquivalentTo(new[] { outboxMessage1, outboxMessage2, outboxMessage3 });
    }

    [Fact]
    public async Task AddAsync_ShouldEnlistInTransaction()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddSqliteOutbox()));

        SilverbackStorageInitializer storageInitializer = serviceProvider.GetRequiredService<SilverbackStorageInitializer>();
        await storageInitializer.CreateSqliteOutboxAsync(_outboxSettings);

        IOutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<IOutboxWriterFactory>();
        IOutboxWriter outboxWriter = writerFactory.GetWriter(_outboxSettings, serviceProvider);

        await outboxWriter.AddAsync(new OutboxMessage(new byte[] { 0x01 }, null, Endpoint));
        await outboxWriter.AddAsync(new OutboxMessage(new byte[] { 0x02 }, null, Endpoint));
        await outboxWriter.AddAsync(new OutboxMessage(new byte[] { 0x03 }, null, Endpoint));

        SqliteConnection connection = new(_outboxSettings.ConnectionString);
        await connection.OpenAsync();

        SilverbackContext context = new();

        await using (DbTransaction transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadUncommitted))
        {
            await using IStorageTransaction storageTransaction = context.EnlistDbTransaction(transaction);

            // Add and rollback
            await outboxWriter.AddAsync(new OutboxMessage(new byte[] { 0x99 }, null, Endpoint), context);
            await transaction.RollbackAsync();
        }

        (await _outboxReader.GetAsync(10)).Should().HaveCount(3);

        // Add after rollback
        await outboxWriter.AddAsync(
            new OutboxMessage(new byte[] { 0x99 }, null, Endpoint),
            context);

        (await _outboxReader.GetAsync(10)).Should().HaveCount(4);

        // Begin new transaction, add and commit
        await using (DbTransaction transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadUncommitted))
        {
            await using IStorageTransaction storageTransaction = context.EnlistDbTransaction(transaction);

            await outboxWriter.AddAsync(new OutboxMessage(new byte[] { 0x99 }, null, Endpoint), context);
            await outboxWriter.AddAsync(new OutboxMessage(new byte[] { 0x99 }, null, Endpoint), context);
            await outboxWriter.AddAsync(new OutboxMessage(new byte[] { 0x99 }, null, Endpoint), context);
            await transaction.CommitAsync();
        }

        (await _outboxReader.GetAsync(10)).Should().HaveCount(7);
    }

    [Fact]
    public async Task AddAsync_ShouldEnlistInTransaction_WhenStoringBatch()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddSqliteOutbox()));

        SilverbackStorageInitializer storageInitializer = serviceProvider.GetRequiredService<SilverbackStorageInitializer>();
        await storageInitializer.CreateSqliteOutboxAsync(_outboxSettings);

        IOutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<IOutboxWriterFactory>();
        IOutboxWriter outboxWriter = writerFactory.GetWriter(_outboxSettings, serviceProvider);

        await outboxWriter.AddAsync(new OutboxMessage(new byte[] { 0x01 }, null, Endpoint));
        await outboxWriter.AddAsync(new OutboxMessage(new byte[] { 0x02 }, null, Endpoint));
        await outboxWriter.AddAsync(new OutboxMessage(new byte[] { 0x03 }, null, Endpoint));

        SqliteConnection connection = new(_outboxSettings.ConnectionString);
        await connection.OpenAsync();

        SilverbackContext context = new();

        await using (DbTransaction transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadUncommitted))
        {
            await using IStorageTransaction storageTransaction = context.EnlistDbTransaction(transaction);

            // Add and rollback
            await outboxWriter.AddAsync(
                new OutboxMessage[]
                {
                    new(new byte[] { 0x99 }, null, Endpoint),
                    new(new byte[] { 0x99 }, null, Endpoint),
                },
                context);
            await transaction.RollbackAsync();
        }

        (await _outboxReader.GetAsync(10)).Should().HaveCount(3);

        // Add after rollback
        await outboxWriter.AddAsync(
            new OutboxMessage(new byte[] { 0x99 }, null, Endpoint),
            context);

        (await _outboxReader.GetAsync(10)).Should().HaveCount(4);

        // Begin new transaction, add and commit
        await using (DbTransaction transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadUncommitted))
        {
            await using IStorageTransaction storageTransaction = context.EnlistDbTransaction(transaction);

            await outboxWriter.AddAsync(
                new OutboxMessage[]
                {
                    new(new byte[] { 0x99 }, null, Endpoint),
                    new(new byte[] { 0x99 }, null, Endpoint),
                    new(new byte[] { 0x99 }, null, Endpoint)
                },
                context);
            await transaction.CommitAsync();
        }

        (await _outboxReader.GetAsync(10)).Should().HaveCount(7);
    }

    [Fact]
    public async Task AddAsync_ShouldEnlistInTransaction_WhenStoringAsyncBatch()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddSqliteOutbox()));

        SilverbackStorageInitializer storageInitializer = serviceProvider.GetRequiredService<SilverbackStorageInitializer>();
        await storageInitializer.CreateSqliteOutboxAsync(_outboxSettings);

        IOutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<IOutboxWriterFactory>();
        IOutboxWriter outboxWriter = writerFactory.GetWriter(_outboxSettings, serviceProvider);

        await outboxWriter.AddAsync(new OutboxMessage(new byte[] { 0x01 }, null, Endpoint));
        await outboxWriter.AddAsync(new OutboxMessage(new byte[] { 0x02 }, null, Endpoint));
        await outboxWriter.AddAsync(new OutboxMessage(new byte[] { 0x03 }, null, Endpoint));

        SqliteConnection connection = new(_outboxSettings.ConnectionString);
        await connection.OpenAsync();

        SilverbackContext context = new();

        await using (DbTransaction transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadUncommitted))
        {
            await using IStorageTransaction storageTransaction = context.EnlistDbTransaction(transaction);

            // Add and rollback
            await outboxWriter.AddAsync(
                new OutboxMessage[]
                {
                    new(new byte[] { 0x99 }, null, Endpoint),
                    new(new byte[] { 0x99 }, null, Endpoint),
                }.ToAsyncEnumerable(),
                context);
            await transaction.RollbackAsync();
        }

        (await _outboxReader.GetAsync(10)).Should().HaveCount(3);

        // Add after rollback
        await outboxWriter.AddAsync(
            new OutboxMessage(new byte[] { 0x99 }, null, Endpoint),
            context);

        (await _outboxReader.GetAsync(10)).Should().HaveCount(4);

        // Begin new transaction, add and commit
        await using (DbTransaction transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadUncommitted))
        {
            await using IStorageTransaction storageTransaction = context.EnlistDbTransaction(transaction);

            await outboxWriter.AddAsync(
                new OutboxMessage[]
                {
                    new(new byte[] { 0x99 }, null, Endpoint),
                    new(new byte[] { 0x99 }, null, Endpoint),
                    new(new byte[] { 0x99 }, null, Endpoint)
                }.ToAsyncEnumerable(),
                context);
            await transaction.CommitAsync();
        }

        (await _outboxReader.GetAsync(10)).Should().HaveCount(7);
    }

    public void Dispose() => _sqliteConnection.Dispose();
}
