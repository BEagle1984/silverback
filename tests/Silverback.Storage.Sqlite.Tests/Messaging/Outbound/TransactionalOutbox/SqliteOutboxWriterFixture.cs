// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Outbound.TransactionalOutbox;
using Silverback.Storage;
using Silverback.Tests.Logging;
using Xunit;
using IsolationLevel = System.Data.IsolationLevel;

namespace Silverback.Tests.Storage.Sqlite.Messaging.Outbound.TransactionalOutbox;

public sealed class SqliteOutboxWriterFixture : IDisposable
{
    private static readonly OutboxMessageEndpoint Endpoint = new("test", null, null);

    private readonly SqliteOutboxSettings _outboxSettings;

    private readonly SqliteConnection _sqliteConnection;

    private readonly SqliteOutboxReader _outboxReader;

    public SqliteOutboxWriterFixture()
    {
        _outboxSettings = new SqliteOutboxSettings($"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared", "TestOutbox");
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
        IOutboxWriter outboxWriter = writerFactory.GetWriter(_outboxSettings);

        OutboxMessage outboxMessage1 = new(typeof(TestMessage), new byte[] { 0x01 }, null, Endpoint);
        OutboxMessage outboxMessage2 = new(typeof(TestMessage), new byte[] { 0x02 }, null, Endpoint);
        OutboxMessage outboxMessage3 = new(typeof(TestMessage), new byte[] { 0x03 }, null, Endpoint);
        await outboxWriter.AddAsync(outboxMessage1);
        await outboxWriter.AddAsync(outboxMessage2);
        await outboxWriter.AddAsync(outboxMessage3);

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
        IOutboxWriter outboxWriter = writerFactory.GetWriter(_outboxSettings);

        await outboxWriter.AddAsync(new OutboxMessage(typeof(TestMessage), new byte[] { 0x01 }, null, Endpoint));
        await outboxWriter.AddAsync(new OutboxMessage(typeof(TestMessage), new byte[] { 0x02 }, null, Endpoint));
        await outboxWriter.AddAsync(new OutboxMessage(typeof(TestMessage), new byte[] { 0x03 }, null, Endpoint));

        SqliteConnection connection = new(_outboxSettings.ConnectionString);
        await connection.OpenAsync();
        DbTransaction transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadUncommitted);
        SilverbackContext context = new();
        context.SetStorageTransaction(transaction);

        // Add and rollback
        await outboxWriter.AddAsync(
            new OutboxMessage(typeof(TestMessage), new byte[] { 0x99 }, null, Endpoint),
            context);
        await transaction.RollbackAsync();

        (await _outboxReader.GetAsync(10)).Should().HaveCount(3);

        // Add after rollback
        await outboxWriter.AddAsync(
            new OutboxMessage(typeof(TestMessage), new byte[] { 0x99 }, null, Endpoint),
            context);

        (await _outboxReader.GetAsync(10)).Should().HaveCount(4);

        // Begin new transaction, add and commit
        transaction = await connection.BeginTransactionAsync();
        context.SetStorageTransaction(transaction);

        await outboxWriter.AddAsync(
            new OutboxMessage(typeof(TestMessage), new byte[] { 0x99 }, null, Endpoint),
            context);
        await outboxWriter.AddAsync(
            new OutboxMessage(typeof(TestMessage), new byte[] { 0x99 }, null, Endpoint),
            context);
        await outboxWriter.AddAsync(
            new OutboxMessage(typeof(TestMessage), new byte[] { 0x99 }, null, Endpoint),
            context);
        await transaction.CommitAsync();

        (await _outboxReader.GetAsync(10)).Should().HaveCount(7);
    }

    public void Dispose() => _sqliteConnection.Dispose();

    [SuppressMessage("", "CA1812", Justification = "Class used for testing")]
    private class TestMessage
    {
    }
}
