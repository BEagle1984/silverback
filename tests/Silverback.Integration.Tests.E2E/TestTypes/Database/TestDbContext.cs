﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.EntityFrameworkCore;
using Silverback.Database.Model;
using Silverback.Messaging.Publishing;

namespace Silverback.Tests.Integration.E2E.TestTypes.Database;

public class TestDbContext : DbContext
{
    // private readonly DbContextEventsPublisher _eventsPublisher;

    public TestDbContext(IPublisher publisher)
    {
        // _eventsPublisher = new DbContextEventsPublisher(publisher, this);
    }

    public TestDbContext(DbContextOptions options, IPublisher publisher)
        : base(options)
    {
        // _eventsPublisher = new DbContextEventsPublisher(publisher, this);
    }

    public DbSet<TestDomainEntity> TestDomainEntities { get; set; } = null!;

    public override int SaveChanges()
        => SaveChanges(true);

    // public override int SaveChanges(bool acceptAllChangesOnSuccess)
    //     => _eventsPublisher.ExecuteSaveTransaction(() => base.SaveChanges(acceptAllChangesOnSuccess));
    //
    // public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    //     => SaveChangesAsync(true, cancellationToken);
    //
    // public override Task<int> SaveChangesAsync(
    //     bool acceptAllChangesOnSuccess,
    //     CancellationToken cancellationToken = default)
    //     => _eventsPublisher.ExecuteSaveTransactionAsync(
    //         () =>
    //             base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InboundLogEntry>()
            .HasKey(t => new { t.MessageId, t.ConsumerGroupName });
    }
}
