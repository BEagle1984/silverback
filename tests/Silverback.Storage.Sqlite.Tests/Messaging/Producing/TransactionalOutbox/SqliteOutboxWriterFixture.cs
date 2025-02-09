// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Storage;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Storage.Sqlite.Messaging.Producing.TransactionalOutbox;

public sealed class SqliteOutboxWriterFixture : IDisposable
{
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

        OutboxMessage outboxMessage1 = new([0x01], null, "test");
        OutboxMessage outboxMessage2 = new([0x02], null, "test");
        OutboxMessage outboxMessage3 = new([0x03], null, "test");
        await outboxWriter.AddAsync(outboxMessage1);
        await outboxWriter.AddAsync(outboxMessage2);
        await outboxWriter.AddAsync(outboxMessage3);

        List<OutboxMessage> dbOutboxMessages = await (await _outboxReader.GetAsync(10)).ToListAsync();
        dbOutboxMessages.Count.ShouldBe(3);
        dbOutboxMessages.ShouldContain(
            dbOutboxMessage => dbOutboxMessage.Content!.SequenceEqual(outboxMessage1.Content!) &&
                               dbOutboxMessage.Headers == null &&
                               dbOutboxMessage.EndpointName == outboxMessage1.EndpointName);
        dbOutboxMessages.ShouldContain(
            dbOutboxMessage => dbOutboxMessage.Content!.SequenceEqual(outboxMessage2.Content!) &&
                               dbOutboxMessage.Headers == null &&
                               dbOutboxMessage.EndpointName == outboxMessage2.EndpointName);
        dbOutboxMessages.ShouldContain(
            dbOutboxMessage => dbOutboxMessage.Content!.SequenceEqual(outboxMessage3.Content!) &&
                               dbOutboxMessage.Headers == null &&
                               dbOutboxMessage.EndpointName == outboxMessage3.EndpointName);
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

        OutboxMessage outboxMessage1 = new([0x01], null, "test");
        OutboxMessage outboxMessage2 = new([0x02], null, "test");
        OutboxMessage outboxMessage3 = new([0x03], null, "test");
        await outboxWriter.AddAsync([outboxMessage1, outboxMessage2, outboxMessage3]);

        List<OutboxMessage> dbOutboxMessages = await (await _outboxReader.GetAsync(10)).ToListAsync();
        dbOutboxMessages.Count.ShouldBe(3);
        dbOutboxMessages.ShouldContain(
            dbOutboxMessage => dbOutboxMessage.Content!.SequenceEqual(outboxMessage1.Content!) &&
                               dbOutboxMessage.Headers == null &&
                               dbOutboxMessage.EndpointName == outboxMessage1.EndpointName);
        dbOutboxMessages.ShouldContain(
            dbOutboxMessage => dbOutboxMessage.Content!.SequenceEqual(outboxMessage2.Content!) &&
                               dbOutboxMessage.Headers == null &&
                               dbOutboxMessage.EndpointName == outboxMessage2.EndpointName);
        dbOutboxMessages.ShouldContain(
            dbOutboxMessage => dbOutboxMessage.Content!.SequenceEqual(outboxMessage3.Content!) &&
                               dbOutboxMessage.Headers == null &&
                               dbOutboxMessage.EndpointName == outboxMessage3.EndpointName);
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

        OutboxMessage outboxMessage1 = new([0x01], null, "test");
        OutboxMessage outboxMessage2 = new([0x02], null, "test");
        OutboxMessage outboxMessage3 = new([0x03], null, "test");
        await outboxWriter.AddAsync(new[] { outboxMessage1, outboxMessage2, outboxMessage3 }.ToAsyncEnumerable());

        List<OutboxMessage> dbOutboxMessages = await (await _outboxReader.GetAsync(10)).ToListAsync();
        dbOutboxMessages.Count.ShouldBe(3);
        dbOutboxMessages.ShouldContain(
            dbOutboxMessage => dbOutboxMessage.Content!.SequenceEqual(outboxMessage1.Content!) &&
                               dbOutboxMessage.Headers == null &&
                               dbOutboxMessage.EndpointName == outboxMessage1.EndpointName);
        dbOutboxMessages.ShouldContain(
            dbOutboxMessage => dbOutboxMessage.Content!.SequenceEqual(outboxMessage2.Content!) &&
                               dbOutboxMessage.Headers == null &&
                               dbOutboxMessage.EndpointName == outboxMessage2.EndpointName);
        dbOutboxMessages.ShouldContain(
            dbOutboxMessage => dbOutboxMessage.Content!.SequenceEqual(outboxMessage3.Content!) &&
                               dbOutboxMessage.Headers == null &&
                               dbOutboxMessage.EndpointName == outboxMessage3.EndpointName);
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

        await outboxWriter.AddAsync(new OutboxMessage([0x01], null, "test"));
        await outboxWriter.AddAsync(new OutboxMessage([0x02], null, "test"));
        await outboxWriter.AddAsync(new OutboxMessage([0x03], null, "test"));

        SqliteConnection connection = new(_outboxSettings.ConnectionString);
        await connection.OpenAsync();

        SilverbackContext context = new(serviceProvider);

        await using (DbTransaction transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadUncommitted))
        {
            await using IStorageTransaction storageTransaction = context.EnlistDbTransaction(transaction);

            // Add and rollback
            await outboxWriter.AddAsync(new OutboxMessage([0x99], null, "test"), context);
            await transaction.RollbackAsync();
        }

        (await (await _outboxReader.GetAsync(10)).ToListAsync()).Count.ShouldBe(3);

        // Add after rollback
        await outboxWriter.AddAsync(
            new OutboxMessage([0x99], null, "test"),
            context);

        (await (await _outboxReader.GetAsync(10)).ToListAsync()).Count.ShouldBe(4);

        // Begin new transaction, add and commit
        await using (DbTransaction transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadUncommitted))
        {
            await using IStorageTransaction storageTransaction = context.EnlistDbTransaction(transaction);

            await outboxWriter.AddAsync(new OutboxMessage([0x99], null, "test"), context);
            await outboxWriter.AddAsync(new OutboxMessage([0x99], null, "test"), context);
            await outboxWriter.AddAsync(new OutboxMessage([0x99], null, "test"), context);
            await transaction.CommitAsync();
        }

        (await (await _outboxReader.GetAsync(10)).ToListAsync()).Count.ShouldBe(7);
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

        await outboxWriter.AddAsync(new OutboxMessage([0x01], null, "test"));
        await outboxWriter.AddAsync(new OutboxMessage([0x02], null, "test"));
        await outboxWriter.AddAsync(new OutboxMessage([0x03], null, "test"));

        SqliteConnection connection = new(_outboxSettings.ConnectionString);
        await connection.OpenAsync();

        SilverbackContext context = new(serviceProvider);

        await using (DbTransaction transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadUncommitted))
        {
            await using IStorageTransaction storageTransaction = context.EnlistDbTransaction(transaction);

            // Add and rollback
            await outboxWriter.AddAsync(
                [
                    new OutboxMessage([0x99], null, "test"),
                    new OutboxMessage([0x99], null, "test"),
                ],
                context);
            await transaction.RollbackAsync();
        }

        (await (await _outboxReader.GetAsync(10)).ToListAsync()).Count.ShouldBe(3);

        // Add after rollback
        await outboxWriter.AddAsync(
            new OutboxMessage([0x99], null, "test"),
            context);

        (await (await _outboxReader.GetAsync(10)).ToListAsync()).Count.ShouldBe(4);

        // Begin new transaction, add and commit
        await using (DbTransaction transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadUncommitted))
        {
            await using IStorageTransaction storageTransaction = context.EnlistDbTransaction(transaction);

            await outboxWriter.AddAsync(
                [
                    new OutboxMessage([0x99], null, "test"),
                    new OutboxMessage([0x99], null, "test"),
                    new OutboxMessage([0x99], null, "test")
                ],
                context);
            await transaction.CommitAsync();
        }

        (await (await _outboxReader.GetAsync(10)).ToListAsync()).Count.ShouldBe(7);
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

        await outboxWriter.AddAsync(new OutboxMessage([0x01], null, "test"));
        await outboxWriter.AddAsync(new OutboxMessage([0x02], null, "test"));
        await outboxWriter.AddAsync(new OutboxMessage([0x03], null, "test"));

        SqliteConnection connection = new(_outboxSettings.ConnectionString);
        await connection.OpenAsync();

        SilverbackContext context = new(serviceProvider);

        await using (DbTransaction transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadUncommitted))
        {
            await using IStorageTransaction storageTransaction = context.EnlistDbTransaction(transaction);

            // Add and rollback
            await outboxWriter.AddAsync(
                new OutboxMessage[]
                {
                    new([0x99], null, "test"),
                    new([0x99], null, "test"),
                }.ToAsyncEnumerable(),
                context);
            await transaction.RollbackAsync();
        }

        (await (await _outboxReader.GetAsync(10)).ToListAsync()).Count.ShouldBe(3);

        // Add after rollback
        await outboxWriter.AddAsync(
            new OutboxMessage([0x99], null, "test"),
            context);

        (await (await _outboxReader.GetAsync(10)).ToListAsync()).Count.ShouldBe(4);

        // Begin new transaction, add and commit
        await using (DbTransaction transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadUncommitted))
        {
            await using IStorageTransaction storageTransaction = context.EnlistDbTransaction(transaction);

            await outboxWriter.AddAsync(
                new OutboxMessage[]
                {
                    new([0x99], null, "test"),
                    new([0x99], null, "test"),
                    new([0x99], null, "test")
                }.ToAsyncEnumerable(),
                context);
            await transaction.CommitAsync();
        }

        (await (await _outboxReader.GetAsync(10)).ToListAsync()).Count.ShouldBe(7);
    }

    public void Dispose() => _sqliteConnection.Dispose();
}
