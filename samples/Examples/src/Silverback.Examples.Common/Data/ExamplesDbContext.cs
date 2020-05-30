// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Silverback.Database.Model;
using Silverback.EntityFrameworkCore;
using Silverback.Messaging.Publishing;

namespace Silverback.Examples.Common.Data
{
    public class ExamplesDbContext : DbContext
    {
        private readonly DbContextEventsPublisher _eventsPublisher;

        public ExamplesDbContext(IPublisher publisher)
        {
            _eventsPublisher = new DbContextEventsPublisher(publisher, this);
        }

        public ExamplesDbContext(DbContextOptions options, IPublisher publisher)
            : base(options)
        {
            _eventsPublisher = new DbContextEventsPublisher(publisher, this);
        }

        public DbSet<OutboundMessage> OutboundMessages { get; set; }
        public DbSet<InboundLogEntry> InboundMessages { get; set; }
        public DbSet<StoredOffset> StoredOffsets { get; set; }
        public DbSet<TemporaryMessageChunk> Chunks { get; set; }
        public DbSet<Lock> Locks { get; set; }

        public DbSet<Customer> Customers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OutboundMessage>()
                .ToTable("Messaging_OutboundMessages");

            modelBuilder.Entity<InboundLogEntry>()
                .ToTable("Messaging_InboundMessages")
                .HasKey(t => new { t.MessageId, t.EndpointName });

            modelBuilder.Entity<StoredOffset>()
                .ToTable("Messaging_StoredOffsets");

            modelBuilder.Entity<TemporaryMessageChunk>()
                .ToTable("Messaging_MessageChunks")
                .HasKey(t => new { t.MessageId, t.ChunkIndex });
        }

        public override int SaveChanges()
            => SaveChanges(true);

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
            => _eventsPublisher.ExecuteSaveTransaction(() => base.SaveChanges(acceptAllChangesOnSuccess));

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => SaveChangesAsync(true, cancellationToken);

        public override Task<int> SaveChangesAsync(
            bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default)
            => _eventsPublisher.ExecuteSaveTransactionAsync(() =>
                base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken));
    }
}