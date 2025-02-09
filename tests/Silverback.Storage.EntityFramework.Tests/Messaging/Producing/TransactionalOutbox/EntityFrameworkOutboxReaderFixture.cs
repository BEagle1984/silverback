// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Tests.Logging;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Storage.EntityFramework.Messaging.Producing.TransactionalOutbox;

public sealed class EntityFrameworkOutboxReaderFixture : IDisposable
{
    private readonly SqliteConnection _sqliteConnection;

    private readonly EntityFrameworkOutboxSettings _outboxSettings;

    private readonly IServiceProvider _serviceProvider;

    private readonly IOutboxWriter _outboxWriter;

    public EntityFrameworkOutboxReaderFixture()
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

        _outboxWriter = _serviceProvider.GetRequiredService<IOutboxWriterFactory>().GetWriter(_outboxSettings, _serviceProvider);
    }

    [Fact]
    public async Task AcknowledgeAsync_ShouldRemoveItemsFromStorage()
    {
        using IServiceScope scope = _serviceProvider.CreateScope();
        TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        DbOutboxMessage outboxMessage1 = new(1, [0x01], null, "test");
        DbOutboxMessage outboxMessage2 = new(2, [0x02], null, "test");
        DbOutboxMessage outboxMessage3 = new(3, [0x03], null, "test");
        DbOutboxMessage outboxMessage4 = new(4, [0x04], null, "test");
        await _outboxWriter.AddAsync(outboxMessage1);
        await _outboxWriter.AddAsync(outboxMessage2);
        await _outboxWriter.AddAsync(outboxMessage3);
        await _outboxWriter.AddAsync(outboxMessage4);

        dbContext.Outbox.AsNoTracking().Count().ShouldBe(4);

        IOutboxReaderFactory readerFactory = _serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(_outboxSettings, _serviceProvider);

        await outboxReader.AcknowledgeAsync([outboxMessage1, outboxMessage3]);

        dbContext.Outbox.AsNoTracking().Count().ShouldBe(2);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnItemsBatch()
    {
        using IServiceScope scope = _serviceProvider.CreateScope();
        TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        await _outboxWriter.AddAsync(new OutboxMessage([0x01], null, "test"));
        await _outboxWriter.AddAsync(new OutboxMessage([0x02], null, "test"));
        await _outboxWriter.AddAsync(new OutboxMessage([0x03], null, "test"));
        await _outboxWriter.AddAsync(new OutboxMessage([0x04], null, "test"));
        await _outboxWriter.AddAsync(new OutboxMessage([0x05], null, "test"));

        IOutboxReaderFactory readerFactory = _serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(_outboxSettings, _serviceProvider);

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
        using IServiceScope scope = _serviceProvider.CreateScope();
        TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        IOutboxReaderFactory readerFactory = _serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(_outboxSettings, _serviceProvider);

        IDisposableAsyncEnumerable<OutboxMessage> messages = await outboxReader.GetAsync(3);

        (await messages.ToListAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAsync_ShouldReturnTheSameItemsIfNotRemoved()
    {
        using IServiceScope scope = _serviceProvider.CreateScope();
        TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        await _outboxWriter.AddAsync(new OutboxMessage([0x01], null, "test"));
        await _outboxWriter.AddAsync(new OutboxMessage([0x02], null, "test"));
        await _outboxWriter.AddAsync(new OutboxMessage([0x03], null, "test"));
        await _outboxWriter.AddAsync(new OutboxMessage([0x04], null, "test"));
        await _outboxWriter.AddAsync(new OutboxMessage([0x05], null, "test"));

        IOutboxReaderFactory readerFactory = _serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(_outboxSettings, _serviceProvider);

        IDisposableAsyncEnumerable<OutboxMessage> batch1 = await outboxReader.GetAsync(3);
        IDisposableAsyncEnumerable<OutboxMessage> batch2 = await outboxReader.GetAsync(3);

        List<OutboxMessage> batch1Messages = await batch1.ToListAsync();
        List<OutboxMessage> batch2Messages = await batch2.ToListAsync();
        batch2Messages.ShouldBeEquivalentTo(batch1Messages);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnStoredItemsInChronologicalOrder()
    {
        using IServiceScope scope = _serviceProvider.CreateScope();
        TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        await _outboxWriter.AddAsync(new OutboxMessage([0x01], null, "test"));
        await _outboxWriter.AddAsync(new OutboxMessage([0x02], null, "test"));
        await _outboxWriter.AddAsync(new OutboxMessage([0x03], null, "test"));

        IOutboxReaderFactory readerFactory = _serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(_outboxSettings, _serviceProvider);

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
        using IServiceScope scope = _serviceProvider.CreateScope();
        TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        await _outboxWriter.AddAsync(new OutboxMessage([0x01], null, "test"));
        await _outboxWriter.AddAsync(new OutboxMessage([0x02], null, "test"));
        await _outboxWriter.AddAsync(new OutboxMessage([0x03], null, "test"));

        IOutboxReaderFactory readerFactory = _serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(_outboxSettings, _serviceProvider);

        int count = await outboxReader.GetLengthAsync();

        count.ShouldBe(3);
    }

    [Fact]
    public async Task GetLengthAsync_ShouldReturnZero_WhenTheStorageIsEmpty()
    {
        using IServiceScope scope = _serviceProvider.CreateScope();
        TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        IOutboxReaderFactory readerFactory = _serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(_outboxSettings, _serviceProvider);

        int count = await outboxReader.GetLengthAsync();

        count.ShouldBe(0);
    }

    [Fact]
    public async Task GetMaxAgeAsync_ShouldReturnOldestItemAge()
    {
        using IServiceScope scope = _serviceProvider.CreateScope();
        TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        await _outboxWriter.AddAsync(new OutboxMessage([0x01], null, "test"));
        await Task.Delay(100);
        await _outboxWriter.AddAsync(new OutboxMessage([0x02], null, "test"));

        IOutboxReaderFactory readerFactory = _serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(_outboxSettings, _serviceProvider);

        TimeSpan maxAge = await outboxReader.GetMaxAgeAsync();

        maxAge.ShouldBeGreaterThan(TimeSpan.FromMilliseconds(99)); // Exact value causes flaky tests on CI pipeline
    }

    [Fact]
    public async Task GetMaxAgeAsync_ShouldReturnZero_WhenTheStorageIsEmpty()
    {
        using IServiceScope scope = _serviceProvider.CreateScope();
        TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        IOutboxReaderFactory readerFactory = _serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(_outboxSettings, _serviceProvider);

        TimeSpan maxAge = await outboxReader.GetMaxAgeAsync();

        maxAge.ShouldBe(TimeSpan.Zero);
    }

    public void Dispose() => _sqliteConnection.Dispose();

    private class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options)
            : base(options)
        {
        }

        public DbSet<SilverbackOutboxMessage> Outbox { get; set; } = null!;
    }
}
