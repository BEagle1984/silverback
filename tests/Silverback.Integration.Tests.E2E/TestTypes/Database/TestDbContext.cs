// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Silverback.Domain;
using Silverback.Messaging.Publishing;

namespace Silverback.Tests.Integration.E2E.TestTypes.Database;

public class TestDbContext : DbContext
{
    private readonly DomainEventsPublisher _eventsPublisher;

    public TestDbContext(IPublisher publisher)
    {
        _eventsPublisher = new DomainEventsPublisher(() => ChangeTracker.Entries().Select(entry => entry.Entity), publisher);
    }

    public TestDbContext(DbContextOptions options, IPublisher publisher)
        : base(options)
    {
        _eventsPublisher = new DomainEventsPublisher(() => ChangeTracker.Entries().Select(entry => entry.Entity), publisher);
    }

    public DbSet<TestDomainEntity> TestDomainEntities { get; set; } = null!;

    public override int SaveChanges() => SaveChanges(true);

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        using TransactionScope transaction = new();
        _eventsPublisher.PublishDomainEvents();
        int result = base.SaveChanges(acceptAllChangesOnSuccess);
        transaction.Complete();
        return result;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        using TransactionScope transaction = new();
        await _eventsPublisher.PublishDomainEventsAsync();
        int result = await base.SaveChangesAsync(cancellationToken);
        transaction.Complete();
        return result;
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        using TransactionScope transaction = new();
        await _eventsPublisher.PublishDomainEventsAsync();
        int result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        transaction.Complete();
        return result;
    }
}
