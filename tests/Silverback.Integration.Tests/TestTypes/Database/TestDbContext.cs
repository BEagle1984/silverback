// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.EntityFrameworkCore;
using Silverback.Database.Model;

namespace Silverback.Tests.Integration.TestTypes.Database;

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions options)
        : base(options)
    {
    }

    public DbSet<OutboxMessage> Outbox { get; set; } = null!;

    public DbSet<InboundLogEntry> InboundMessages { get; set; } = null!;

    public DbSet<StoredOffset> StoredOffsets { get; set; } = null!;

    public DbSet<Lock> Locks { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InboundLogEntry>()
            .HasKey(t => new { t.MessageId, t.ConsumerGroupName });
    }
}
