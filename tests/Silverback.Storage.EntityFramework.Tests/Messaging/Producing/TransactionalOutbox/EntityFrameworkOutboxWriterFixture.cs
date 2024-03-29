// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Storage;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Storage.EntityFramework.Messaging.Producing.TransactionalOutbox;

public sealed class EntityFrameworkOutboxWriterFixture : IDisposable
{
    private static readonly OutboxMessageEndpoint Endpoint = new("test", null);

    private readonly SqliteConnection _sqliteConnection;

    private readonly EntityFrameworkOutboxSettings _outboxSettings;

    private readonly IServiceProvider _serviceProvider;

    private readonly IOutboxReader _outboxReader;

    public EntityFrameworkOutboxWriterFixture()
    {
        _sqliteConnection = new SqliteConnection($"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        _sqliteConnection.Open();

        _outboxSettings = new EntityFrameworkOutboxSettings(
            typeof(TestDbContext),
            (serviceProvider, _) => serviceProvider.GetRequiredService<TestDbContext>());

        _serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddDbContext<TestDbContext>(options => options.UseSqlite(_sqliteConnection))
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddEntityFrameworkOutbox()));

        _outboxReader = _serviceProvider.GetRequiredService<IOutboxReaderFactory>().GetReader(_outboxSettings, _serviceProvider);
    }

    [Fact]
    public async Task AddAsync_ShouldAddItemToStorage()
    {
        using IServiceScope scope = _serviceProvider.CreateScope();
        TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        IOutboxWriterFactory writerFactory = _serviceProvider.GetRequiredService<IOutboxWriterFactory>();
        IOutboxWriter outboxWriter = writerFactory.GetWriter(_outboxSettings, _serviceProvider);

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
        using IServiceScope scope = _serviceProvider.CreateScope();
        TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        IOutboxWriterFactory writerFactory = _serviceProvider.GetRequiredService<IOutboxWriterFactory>();
        IOutboxWriter outboxWriter = writerFactory.GetWriter(_outboxSettings, _serviceProvider);

        OutboxMessage outboxMessage1 = new(new byte[] { 0x01 }, null, Endpoint);
        OutboxMessage outboxMessage2 = new(new byte[] { 0x02 }, null, Endpoint);
        OutboxMessage outboxMessage3 = new(new byte[] { 0x03 }, null, Endpoint);
        await outboxWriter.AddAsync(new[] { outboxMessage1, outboxMessage2, outboxMessage3 });

        (await _outboxReader.GetAsync(10)).Should().BeEquivalentTo(new[] { outboxMessage1, outboxMessage2, outboxMessage3 });
    }

    [Fact]
    public async Task AddAsync_ShouldAddAsyncItemsToStorage()
    {
        using IServiceScope scope = _serviceProvider.CreateScope();
        TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        IOutboxWriterFactory writerFactory = _serviceProvider.GetRequiredService<IOutboxWriterFactory>();
        IOutboxWriter outboxWriter = writerFactory.GetWriter(_outboxSettings, _serviceProvider);

        OutboxMessage outboxMessage1 = new(new byte[] { 0x01 }, null, Endpoint);
        OutboxMessage outboxMessage2 = new(new byte[] { 0x02 }, null, Endpoint);
        OutboxMessage outboxMessage3 = new(new byte[] { 0x03 }, null, Endpoint);
        await outboxWriter.AddAsync(new[] { outboxMessage1, outboxMessage2, outboxMessage3 }.ToAsyncEnumerable());

        (await _outboxReader.GetAsync(10)).Should().BeEquivalentTo(new[] { outboxMessage1, outboxMessage2, outboxMessage3 });
    }

    [Fact]
    public async Task AddAsync_ShouldEnlistInTransaction()
    {
        using IServiceScope scope = _serviceProvider.CreateScope();
        TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        IOutboxWriterFactory writerFactory = _serviceProvider.GetRequiredService<IOutboxWriterFactory>();
        IOutboxWriter outboxWriter = writerFactory.GetWriter(_outboxSettings, _serviceProvider);

        await outboxWriter.AddAsync(new OutboxMessage(new byte[] { 0x01 }, null, Endpoint));
        await outboxWriter.AddAsync(new OutboxMessage(new byte[] { 0x02 }, null, Endpoint));
        await outboxWriter.AddAsync(new OutboxMessage(new byte[] { 0x03 }, null, Endpoint));

        SilverbackContext context = new();

        await using (IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadUncommitted))
        {
            await using IStorageTransaction storageTransaction = context.EnlistDbTransaction(transaction.GetDbTransaction());

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
        await using (IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadUncommitted))
        {
            await using IStorageTransaction storageTransaction = context.EnlistDbTransaction(transaction.GetDbTransaction());

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
        using IServiceScope scope = _serviceProvider.CreateScope();
        TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        IOutboxWriterFactory writerFactory = _serviceProvider.GetRequiredService<IOutboxWriterFactory>();
        IOutboxWriter outboxWriter = writerFactory.GetWriter(_outboxSettings, _serviceProvider);

        await outboxWriter.AddAsync(new OutboxMessage(new byte[] { 0x01 }, null, Endpoint));
        await outboxWriter.AddAsync(new OutboxMessage(new byte[] { 0x02 }, null, Endpoint));
        await outboxWriter.AddAsync(new OutboxMessage(new byte[] { 0x03 }, null, Endpoint));

        SilverbackContext context = new();

        await using (IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadUncommitted))
        {
            await using IStorageTransaction storageTransaction = context.EnlistDbTransaction(transaction.GetDbTransaction());

            // Add and rollback
            await outboxWriter.AddAsync(
                new OutboxMessage[]
                {
                    new(new byte[] { 0x99 }, null, Endpoint),
                    new(new byte[] { 0x99 }, null, Endpoint)
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
        await using (IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadUncommitted))
        {
            await using IStorageTransaction storageTransaction = context.EnlistDbTransaction(transaction.GetDbTransaction());

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
        using IServiceScope scope = _serviceProvider.CreateScope();
        TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        IOutboxWriterFactory writerFactory = scope.ServiceProvider.GetRequiredService<IOutboxWriterFactory>();
        IOutboxWriter outboxWriter = writerFactory.GetWriter(_outboxSettings, _serviceProvider);

        await outboxWriter.AddAsync(new OutboxMessage(new byte[] { 0x01 }, null, Endpoint));
        await outboxWriter.AddAsync(new OutboxMessage(new byte[] { 0x02 }, null, Endpoint));
        await outboxWriter.AddAsync(new OutboxMessage(new byte[] { 0x03 }, null, Endpoint));

        SilverbackContext context = new();

        await using (IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadUncommitted))
        {
            await using IStorageTransaction storageTransaction = context.EnlistDbTransaction(transaction.GetDbTransaction());

            // Add and rollback
            await outboxWriter.AddAsync(
                new OutboxMessage[]
                {
                    new(new byte[] { 0x99 }, null, Endpoint),
                    new(new byte[] { 0x99 }, null, Endpoint)
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
        await using (IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadUncommitted))
        {
            await using IStorageTransaction storageTransaction = context.EnlistDbTransaction(transaction.GetDbTransaction());

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

    private class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options)
            : base(options)
        {
        }

        public DbSet<SilverbackOutboxMessage> Outbox { get; } = null!;
    }
}
