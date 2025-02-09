// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Storage;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Storage.EntityFramework.Messaging.Producing.TransactionalOutbox;

public sealed class EntityFrameworkOutboxWriterFixture : IDisposable
{
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
        using IServiceScope scope = _serviceProvider.CreateScope();
        TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        IOutboxWriterFactory writerFactory = _serviceProvider.GetRequiredService<IOutboxWriterFactory>();
        IOutboxWriter outboxWriter = writerFactory.GetWriter(_outboxSettings, _serviceProvider);

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
        using IServiceScope scope = _serviceProvider.CreateScope();
        TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        IOutboxWriterFactory writerFactory = _serviceProvider.GetRequiredService<IOutboxWriterFactory>();
        IOutboxWriter outboxWriter = writerFactory.GetWriter(_outboxSettings, _serviceProvider);

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
        using IServiceScope scope = _serviceProvider.CreateScope();
        TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        IOutboxWriterFactory writerFactory = _serviceProvider.GetRequiredService<IOutboxWriterFactory>();
        IOutboxWriter outboxWriter = writerFactory.GetWriter(_outboxSettings, _serviceProvider);

        await outboxWriter.AddAsync(new OutboxMessage([0x01], null, "test"));
        await outboxWriter.AddAsync(new OutboxMessage([0x02], null, "test"));
        await outboxWriter.AddAsync(new OutboxMessage([0x03], null, "test"));

        SilverbackContext context = new(_serviceProvider);

        await using (IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadUncommitted))
        {
            await using IStorageTransaction storageTransaction = context.EnlistDbTransaction(transaction.GetDbTransaction());

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
        await using (IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadUncommitted))
        {
            await using IStorageTransaction storageTransaction = context.EnlistDbTransaction(transaction.GetDbTransaction());

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
        using IServiceScope scope = _serviceProvider.CreateScope();
        TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        IOutboxWriterFactory writerFactory = _serviceProvider.GetRequiredService<IOutboxWriterFactory>();
        IOutboxWriter outboxWriter = writerFactory.GetWriter(_outboxSettings, _serviceProvider);

        await outboxWriter.AddAsync(new OutboxMessage([0x01], null, "test"));
        await outboxWriter.AddAsync(new OutboxMessage([0x02], null, "test"));
        await outboxWriter.AddAsync(new OutboxMessage([0x03], null, "test"));

        SilverbackContext context = new(_serviceProvider);

        await using (IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadUncommitted))
        {
            await using IStorageTransaction storageTransaction = context.EnlistDbTransaction(transaction.GetDbTransaction());

            // Add and rollback
            await outboxWriter.AddAsync(
                [
                    new OutboxMessage([0x99], null, "test"),
                    new OutboxMessage([0x99], null, "test")
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
        await using (IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadUncommitted))
        {
            await using IStorageTransaction storageTransaction = context.EnlistDbTransaction(transaction.GetDbTransaction());

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
        using IServiceScope scope = _serviceProvider.CreateScope();
        TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        IOutboxWriterFactory writerFactory = scope.ServiceProvider.GetRequiredService<IOutboxWriterFactory>();
        IOutboxWriter outboxWriter = writerFactory.GetWriter(_outboxSettings, _serviceProvider);

        await outboxWriter.AddAsync(new OutboxMessage([0x01], null, "test"));
        await outboxWriter.AddAsync(new OutboxMessage([0x02], null, "test"));
        await outboxWriter.AddAsync(new OutboxMessage([0x03], null, "test"));

        SilverbackContext context = new(_serviceProvider);

        await using (IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadUncommitted))
        {
            await using IStorageTransaction storageTransaction = context.EnlistDbTransaction(transaction.GetDbTransaction());

            // Add and rollback
            await outboxWriter.AddAsync(
                new OutboxMessage[]
                {
                    new([0x99], null, "test"),
                    new([0x99], null, "test")
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
        await using (IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadUncommitted))
        {
            await using IStorageTransaction storageTransaction = context.EnlistDbTransaction(transaction.GetDbTransaction());

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

    private class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options)
            : base(options)
        {
        }

        public DbSet<SilverbackOutboxMessage> Outbox { get; } = null!;
    }
}
