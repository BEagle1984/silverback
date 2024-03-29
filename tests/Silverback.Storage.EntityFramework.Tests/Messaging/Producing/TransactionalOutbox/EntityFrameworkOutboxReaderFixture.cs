// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Storage.EntityFramework.Messaging.Producing.TransactionalOutbox;

public sealed class EntityFrameworkOutboxReaderFixture : IDisposable
{
    private static readonly OutboxMessageEndpoint Endpoint = new("test", null);

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

        DbOutboxMessage outboxMessage1 = new(1, new byte[] { 0x01 }, null, Endpoint);
        DbOutboxMessage outboxMessage2 = new(2, new byte[] { 0x02 }, null, Endpoint);
        DbOutboxMessage outboxMessage3 = new(3, new byte[] { 0x03 }, null, Endpoint);
        DbOutboxMessage outboxMessage4 = new(4, new byte[] { 0x04 }, null, Endpoint);
        await _outboxWriter.AddAsync(outboxMessage1);
        await _outboxWriter.AddAsync(outboxMessage2);
        await _outboxWriter.AddAsync(outboxMessage3);
        await _outboxWriter.AddAsync(outboxMessage4);

        dbContext.Outbox.AsNoTracking().Count().Should().Be(4);

        IOutboxReaderFactory readerFactory = _serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(_outboxSettings, _serviceProvider);

        await outboxReader.AcknowledgeAsync(new[] { outboxMessage1, outboxMessage3 });

        dbContext.Outbox.AsNoTracking().Count().Should().Be(2);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnItemsBatch()
    {
        using IServiceScope scope = _serviceProvider.CreateScope();
        TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        await _outboxWriter.AddAsync(new OutboxMessage(new byte[] { 0x01 }, null, Endpoint));
        await _outboxWriter.AddAsync(new OutboxMessage(new byte[] { 0x02 }, null, Endpoint));
        await _outboxWriter.AddAsync(new OutboxMessage(new byte[] { 0x03 }, null, Endpoint));
        await _outboxWriter.AddAsync(new OutboxMessage(new byte[] { 0x04 }, null, Endpoint));
        await _outboxWriter.AddAsync(new OutboxMessage(new byte[] { 0x05 }, null, Endpoint));

        IOutboxReaderFactory readerFactory = _serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(_outboxSettings, _serviceProvider);

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
        using IServiceScope scope = _serviceProvider.CreateScope();
        TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        IOutboxReaderFactory readerFactory = _serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(_outboxSettings, _serviceProvider);

        IReadOnlyCollection<OutboxMessage> messages = await outboxReader.GetAsync(3);

        messages.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAsync_ShouldReturnTheSameItemsIfNotRemoved()
    {
        using IServiceScope scope = _serviceProvider.CreateScope();
        TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        await _outboxWriter.AddAsync(new OutboxMessage(new byte[] { 0x01 }, null, Endpoint));
        await _outboxWriter.AddAsync(new OutboxMessage(new byte[] { 0x02 }, null, Endpoint));
        await _outboxWriter.AddAsync(new OutboxMessage(new byte[] { 0x03 }, null, Endpoint));
        await _outboxWriter.AddAsync(new OutboxMessage(new byte[] { 0x04 }, null, Endpoint));
        await _outboxWriter.AddAsync(new OutboxMessage(new byte[] { 0x05 }, null, Endpoint));

        IOutboxReaderFactory readerFactory = _serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(_outboxSettings, _serviceProvider);

        IReadOnlyCollection<OutboxMessage> batch1 = await outboxReader.GetAsync(3);
        IReadOnlyCollection<OutboxMessage> batch2 = await outboxReader.GetAsync(3);

        batch2.Should().BeEquivalentTo(batch1);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnStoredItemsInChronologicalOrder()
    {
        using IServiceScope scope = _serviceProvider.CreateScope();
        TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        await _outboxWriter.AddAsync(new OutboxMessage(new byte[] { 0x01 }, null, Endpoint));
        await _outboxWriter.AddAsync(new OutboxMessage(new byte[] { 0x02 }, null, Endpoint));
        await _outboxWriter.AddAsync(new OutboxMessage(new byte[] { 0x03 }, null, Endpoint));

        IOutboxReaderFactory readerFactory = _serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(_outboxSettings, _serviceProvider);

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
        using IServiceScope scope = _serviceProvider.CreateScope();
        TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        await _outboxWriter.AddAsync(new OutboxMessage(new byte[] { 0x01 }, null, Endpoint));
        await _outboxWriter.AddAsync(new OutboxMessage(new byte[] { 0x02 }, null, Endpoint));
        await _outboxWriter.AddAsync(new OutboxMessage(new byte[] { 0x03 }, null, Endpoint));

        IOutboxReaderFactory readerFactory = _serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(_outboxSettings, _serviceProvider);

        int count = await outboxReader.GetLengthAsync();

        count.Should().Be(3);
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

        count.Should().Be(0);
    }

    [Fact]
    public async Task GetMaxAgeAsync_ShouldReturnOldestItemAge()
    {
        using IServiceScope scope = _serviceProvider.CreateScope();
        TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        await _outboxWriter.AddAsync(new OutboxMessage(new byte[] { 0x01 }, null, Endpoint));
        await Task.Delay(100);
        await _outboxWriter.AddAsync(new OutboxMessage(new byte[] { 0x02 }, null, Endpoint));

        IOutboxReaderFactory readerFactory = _serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxReader outboxReader = readerFactory.GetReader(_outboxSettings, _serviceProvider);

        TimeSpan maxAge = await outboxReader.GetMaxAgeAsync();

        maxAge.Should().BeGreaterThan(TimeSpan.FromMilliseconds(99)); // Exact value causes flaky tests on CI pipeline
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

        maxAge.Should().Be(TimeSpan.Zero);
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
