// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Storage;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Storage.PostgreSql.Messaging.Producing.TransactionalOutbox;

public sealed class PostgreSqlOutboxWriterFixture : PostgresContainerFixture
{
    private static readonly OutboxMessageEndpoint Endpoint = new("test", null, null);

    private readonly PostgreSqlOutboxSettings _outboxSettings;

    private readonly PostgreSqlOutboxReader _outboxReader;

    public PostgreSqlOutboxWriterFixture()
    {
        _outboxSettings = new PostgreSqlOutboxSettings(ConnectionString, "TestOutbox");
        _outboxReader = new PostgreSqlOutboxReader(_outboxSettings);
    }

    [Fact]
    public async Task AddAsync_ShouldAddItemToStorage()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddPostgreSqlOutbox()));

        SilverbackStorageInitializer storageInitializer = serviceProvider.GetRequiredService<SilverbackStorageInitializer>();
        await storageInitializer.CreatePostgreSqlOutboxAsync(_outboxSettings);

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
                .WithConnectionToMessageBroker(options => options.AddPostgreSqlOutbox()));

        SilverbackStorageInitializer storageInitializer = serviceProvider.GetRequiredService<SilverbackStorageInitializer>();
        await storageInitializer.CreatePostgreSqlOutboxAsync(_outboxSettings);

        IOutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<IOutboxWriterFactory>();
        IOutboxWriter outboxWriter = writerFactory.GetWriter(_outboxSettings);

        await outboxWriter.AddAsync(new OutboxMessage(typeof(TestMessage), new byte[] { 0x01 }, null, Endpoint));
        await outboxWriter.AddAsync(new OutboxMessage(typeof(TestMessage), new byte[] { 0x02 }, null, Endpoint));
        await outboxWriter.AddAsync(new OutboxMessage(typeof(TestMessage), new byte[] { 0x03 }, null, Endpoint));

        NpgsqlConnection connection = new(_outboxSettings.ConnectionString);
        await connection.OpenAsync();
        DbTransaction transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadUncommitted);
        SilverbackContext context = new();
        context.EnlistTransaction(transaction);

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
        context.EnlistTransaction(transaction);

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

    private class TestMessage
    {
    }
}
