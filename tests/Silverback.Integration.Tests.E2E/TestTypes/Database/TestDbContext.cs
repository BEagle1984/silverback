// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Silverback.Domain;
using Silverback.Messaging.Publishing;
using Silverback.Storage;

namespace Silverback.Tests.Integration.E2E.TestTypes.Database;

// TODO: Delete?
public class TestDbContext : DbContext
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
        bool mustCommit = false;
        DbTransaction? transaction = Database.CurrentTransaction?.GetDbTransaction();

        if (transaction == null)
        {
            transaction = Database.BeginTransaction().GetDbTransaction();
            mustCommit = true;
        }

        using IStorageTransaction storageTransaction = _publisher.EnlistDbTransaction(transaction);

        _eventsPublisher.PublishDomainEvents();
        int result = base.SaveChanges(acceptAllChangesOnSuccess);

        if (mustCommit)
            Database.CurrentTransaction!.Commit();

        return result;
    }

    // TODO: Test existing and implicit transaction
    // TODO: Review pattern and see if we can simplify this
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        bool mustCommit = false;
        DbTransaction? transaction = Database.CurrentTransaction?.GetDbTransaction();

        if (transaction == null)
        {
            transaction = (await Database.BeginTransactionAsync(cancellationToken)).GetDbTransaction();
            mustCommit = true;
        }

        await using IStorageTransaction storageTransaction = _publisher.EnlistDbTransaction(transaction);

        await _eventsPublisher.PublishDomainEventsAsync();
        int result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

        if (mustCommit)
            await Database.CurrentTransaction!.CommitAsync(cancellationToken);

        return result;
    }
}
