// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Silverback.Domain;
using Silverback.Lock;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class DomainEventsFixture : KafkaFixture
{
    public DomainEventsFixture(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    private class TestDbContext : DbContext
    {
        private readonly EntityFrameworkDomainEventsPublisher<TestDbContext>? _eventsPublisher;

        public TestDbContext(SqliteConnection connection)
            : base(new DbContextOptionsBuilder().UseSqlite(connection).Options)
        {
        }

        public TestDbContext(IPublisher publisher)
        {
            _eventsPublisher =
                new EntityFrameworkDomainEventsPublisher<TestDbContext>(this, base.SaveChanges, base.SaveChangesAsync, publisher);
        }

        public TestDbContext(DbContextOptions options, IPublisher publisher)
            : base(options)
        {
            _eventsPublisher =
                new EntityFrameworkDomainEventsPublisher<TestDbContext>(this, base.SaveChanges, base.SaveChangesAsync, publisher);
        }

        public DbSet<TestDomainEntity> TestDomainEntities { get; set; } = null!;

        public DbSet<SilverbackOutboxMessage> Outbox { get; set; } = null!;

        public DbSet<SilverbackLock> Locks { get; set; } = null!;

        public override int SaveChanges() => SaveChanges(true);

        public override int SaveChanges(bool acceptAllChangesOnSuccess) =>
            _eventsPublisher?.SaveChangesAndPublishDomainEvents(acceptAllChangesOnSuccess) ?? base.SaveChanges(acceptAllChangesOnSuccess);

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) =>
            _eventsPublisher?.SaveChangesAndPublishDomainEventsAsync(acceptAllChangesOnSuccess, cancellationToken) ??
            base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
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
