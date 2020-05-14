// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.EntityFrameworkCore;
using Silverback.Background.Model;
using Silverback.Messaging.Connectors.Model;
using Silverback.Messaging.LargeMessages;

namespace Silverback.Tests.Integration.E2E.TestTypes.Database
{
    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<OutboundMessage> OutboundMessages { get; set; } = null!;

        public DbSet<InboundMessage> InboundMessages { get; set; } = null!;

        public DbSet<StoredOffset> StoredOffsets { get; set; } = null!;

        public DbSet<TemporaryMessageChunk> Chunks { get; set; } = null!;

        public DbSet<Lock> Locks { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<InboundMessage>()
                .HasKey(t => new { t.MessageId, t.ConsumerGroupName });

            modelBuilder.Entity<TemporaryMessageChunk>()
                .HasKey(t => new { t.OriginalMessageId, t.ChunkId });
        }
    }
}
