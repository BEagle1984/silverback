// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Domain;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Storage;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestHost.Database;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

[SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Test code")]
public class DomainEventsSqliteFixture : KafkaFixture
{
    public DomainEventsSqliteFixture(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task DomainEvents_ShouldBeProducedDuringSaveChangesAsync()
    {
        using SqliteDatabase database = await SqliteDatabase.StartAsync();

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddDbContext<TestDbContext>(options => options.UseSqlite(database.ConnectionString))
                .AddSilverback()
                .AddDelegateSubscriber<ValueChangedDomainEvent, TestEventOne>(HandleDomainEvent)
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer.Produce<IIntegrationEvent>(
                                endpoint => endpoint
                                    .ProduceTo(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        static TestEventOne HandleDomainEvent(ValueChangedDomainEvent domainEvent) => new() { ContentEventOne = $"new value: {domainEvent.Source?.Value}" };

        using (IServiceScope scope = Host.ServiceProvider.CreateScope())
        {
            TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
        }

        using (IServiceScope scope = Host.ServiceProvider.CreateScope())
        {
            TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            TestDomainEntity entity = dbContext.TestDomainEntities.Add(new TestDomainEntity()).Entity;
            entity.SetValue(42);
            await dbContext.SaveChangesAsync();
        }

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.OutboundEnvelopes[0].Message.Should().BeOfType<TestEventOne>();
        Helper.Spy.OutboundEnvelopes[0].Message.As<TestEventOne>().ContentEventOne.Should().Be("new value: 42");

        using (IServiceScope scope = Host.ServiceProvider.CreateScope())
        {
            TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            TestDomainEntity entity = dbContext.TestDomainEntities.First();
            entity.SetValue(42000);
            await dbContext.SaveChangesAsync();
        }

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.OutboundEnvelopes[1].Message.Should().BeOfType<TestEventOne>();
        Helper.Spy.OutboundEnvelopes[1].Message.As<TestEventOne>().ContentEventOne.Should().Be("new value: 42000");
    }

    [Fact]
    public async Task DomainEvents_ShouldBeProducedDuringSaveChangesAsync_WhenTransactionExists()
    {
        using SqliteDatabase database = await SqliteDatabase.StartAsync();

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddDbContext<TestDbContext>(options => options.UseSqlite(database.ConnectionString))
                .AddSilverback()
                .AddDelegateSubscriber<ValueChangedDomainEvent, TestEventOne>(HandleDomainEvent)
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer.Produce<IIntegrationEvent>(
                                endpoint => endpoint
                                    .ProduceTo(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        static TestEventOne HandleDomainEvent(ValueChangedDomainEvent domainEvent) => new() { ContentEventOne = $"new value: {domainEvent.Source?.Value}" };

        using (IServiceScope scope = Host.ServiceProvider.CreateScope())
        {
            TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
        }

        using (IServiceScope scope = Host.ServiceProvider.CreateScope())
        {
            TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            TestDomainEntity entity = dbContext.TestDomainEntities.Add(new TestDomainEntity()).Entity;
            entity.SetValue(42);
            IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync();

            await dbContext.SaveChangesAsync();

            await transaction.RollbackAsync();
        }

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);

        using (IServiceScope scope = Host.ServiceProvider.CreateScope())
        {
            TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            dbContext.TestDomainEntities.Should().HaveCount(0);
        }

        using (IServiceScope scope = Host.ServiceProvider.CreateScope())
        {
            TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            TestDomainEntity entity = dbContext.TestDomainEntities.Add(new TestDomainEntity()).Entity;
            entity.SetValue(42);
            IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync();

            await dbContext.SaveChangesAsync();

            await transaction.CommitAsync();
        }

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);

        using (IServiceScope scope = Host.ServiceProvider.CreateScope())
        {
            TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            dbContext.TestDomainEntities.Should().HaveCount(1);
        }
    }

    [Fact]
    public async Task DomainEvents_ShouldBeProducedDuringSaveChanges()
    {
        using SqliteDatabase database = await SqliteDatabase.StartAsync();

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddDbContext<TestDbContext>(options => options.UseSqlite(database.ConnectionString))
                .AddSilverback()
                .AddDelegateSubscriber<ValueChangedDomainEvent, TestEventOne>(HandleDomainEvent)
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer.Produce<IIntegrationEvent>(
                                endpoint => endpoint
                                    .ProduceTo(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        static TestEventOne HandleDomainEvent(ValueChangedDomainEvent domainEvent) => new() { ContentEventOne = $"new value: {domainEvent.Source?.Value}" };

        using (IServiceScope scope = Host.ServiceProvider.CreateScope())
        {
            TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
        }

        using (IServiceScope scope = Host.ServiceProvider.CreateScope())
        {
            TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            TestDomainEntity entity = dbContext.TestDomainEntities.Add(new TestDomainEntity()).Entity;
            entity.SetValue(42);
            dbContext.SaveChanges();
        }

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.OutboundEnvelopes[0].Message.Should().BeOfType<TestEventOne>();
        Helper.Spy.OutboundEnvelopes[0].Message.As<TestEventOne>().ContentEventOne.Should().Be("new value: 42");

        using (IServiceScope scope = Host.ServiceProvider.CreateScope())
        {
            TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            TestDomainEntity entity = dbContext.TestDomainEntities.First();
            entity.SetValue(42000);
            dbContext.SaveChanges();
        }

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.OutboundEnvelopes[1].Message.Should().BeOfType<TestEventOne>();
        Helper.Spy.OutboundEnvelopes[1].Message.As<TestEventOne>().ContentEventOne.Should().Be("new value: 42000");
    }

    [Fact]
    public async Task DomainEvents_ShouldBeProducedDuringSaveChanges_WhenTransactionExists()
    {
        using SqliteDatabase database = await SqliteDatabase.StartAsync();

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddDbContext<TestDbContext>(options => options.UseSqlite(database.ConnectionString))
                .AddSilverback()
                .AddDelegateSubscriber<ValueChangedDomainEvent, TestEventOne>(HandleDomainEvent)
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer.Produce<IIntegrationEvent>(
                                endpoint => endpoint
                                    .ProduceTo(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        static TestEventOne HandleDomainEvent(ValueChangedDomainEvent domainEvent) => new() { ContentEventOne = $"new value: {domainEvent.Source?.Value}" };

        using (IServiceScope scope = Host.ServiceProvider.CreateScope())
        {
            TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
        }

        using (IServiceScope scope = Host.ServiceProvider.CreateScope())
        {
            TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            TestDomainEntity entity = dbContext.TestDomainEntities.Add(new TestDomainEntity()).Entity;
            entity.SetValue(42);
            IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync();

            dbContext.SaveChanges();

            await transaction.RollbackAsync();
        }

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);

        using (IServiceScope scope = Host.ServiceProvider.CreateScope())
        {
            TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            dbContext.TestDomainEntities.Should().HaveCount(0);
        }

        using (IServiceScope scope = Host.ServiceProvider.CreateScope())
        {
            TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            TestDomainEntity entity = dbContext.TestDomainEntities.Add(new TestDomainEntity()).Entity;
            entity.SetValue(42);
            IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync();

            dbContext.SaveChanges();

            await transaction.CommitAsync();
        }

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);

        using (IServiceScope scope = Host.ServiceProvider.CreateScope())
        {
            TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            dbContext.TestDomainEntities.Should().HaveCount(1);
        }
    }

    private class TestDbContext : DbContext
    {
        private readonly IPublisher _publisher;

        private readonly DomainEventsPublisher _eventsPublisher;

        public TestDbContext(IPublisher publisher)
        {
            _publisher = publisher;
            _eventsPublisher = new DomainEventsPublisher(() => ChangeTracker.Entries().Select(entry => entry.Entity), publisher);
        }

        public TestDbContext(DbContextOptions options, IPublisher publisher)
            : base(options)
        {
            _publisher = publisher;
            _eventsPublisher = new DomainEventsPublisher(() => ChangeTracker.Entries().Select(entry => entry.Entity), publisher);
        }

        public DbSet<TestDomainEntity> TestDomainEntities { get; set; } = null!;

        public override int SaveChanges() => SaveChanges(true);

        // TODO: Test existing and implicit transaction
        // TODO: Review pattern and see if we can simplify this
        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            bool ownTransaction = false;
            DbTransaction? transaction = Database.CurrentTransaction?.GetDbTransaction();

            if (transaction == null)
            {
                transaction = Database.BeginTransaction().GetDbTransaction();
                ownTransaction = true;
            }

            // TODO: Test with outbox
            IStorageTransaction storageTransaction = _publisher.EnlistDbTransaction(transaction);

            try
            {
                _eventsPublisher.PublishDomainEvents();
                int result = base.SaveChanges(acceptAllChangesOnSuccess);

                if (ownTransaction)
                    Database.CurrentTransaction!.Commit();

                return result;
            }
            finally
            {
                if (ownTransaction)
                    storageTransaction.Dispose();
            }
        }

        // TODO: Test existing and implicit transaction
        // TODO: Review pattern and see if we can simplify this
        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            bool ownTransaction = false;
            DbTransaction? transaction = Database.CurrentTransaction?.GetDbTransaction();

            if (transaction == null)
            {
                transaction = (await Database.BeginTransactionAsync(cancellationToken)).GetDbTransaction();
                ownTransaction = true;
            }

            // TODO: Test with outbox
            IStorageTransaction storageTransaction = _publisher.EnlistDbTransaction(transaction);

            try
            {
                await _eventsPublisher.PublishDomainEventsAsync();
                int result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

                if (ownTransaction)
                    await Database.CurrentTransaction!.CommitAsync(cancellationToken);

                return result;
            }
            finally
            {
                if (ownTransaction)
                    await storageTransaction.DisposeAsync();
            }
        }
    }

    private class TestDomainEntity : DomainEntity
    {
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Required by EF Core")]
        public int Id { get; private set; }

        public int Value { get; private set; }

        public void SetValue(int newValue)
        {
            Value = newValue;
            AddEvent<ValueChangedDomainEvent>();
        }
    }

    private class ValueChangedDomainEvent : DomainEvent<TestDomainEntity>;
}
