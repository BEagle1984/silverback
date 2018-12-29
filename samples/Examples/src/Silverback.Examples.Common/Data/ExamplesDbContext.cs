// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Silverback.EntityFrameworkCore;
using Silverback.Messaging.Connectors.Model;
using Silverback.Messaging.Publishing;

namespace Silverback.Examples.Common.Data
{
    public class ExamplesDbContext : DbContext
    {
        private readonly IEventPublisher _eventPublisher;

        public ExamplesDbContext(IEventPublisher eventPublisher)
        {
            _eventPublisher = eventPublisher;
        }

        public ExamplesDbContext(DbContextOptions options, IEventPublisher eventPublisher)
            : base(options)
        {
            _eventPublisher = eventPublisher;
        }

        public DbSet<OutboundMessage> OutboundMessages { get; set; }
        public DbSet<InboundMessage> InboundMessages { get; set; }
        public DbSet<Customer> Customers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OutboundMessage>()
                .ToTable("Messaging_OutboundMessages");

            modelBuilder.Entity<InboundMessage>()
                .ToTable("Messaging_InboundMessages")
                .HasKey(t => new { t.MessageId, t.EndpointName });
        }

        public override int SaveChanges()
            => SaveChanges(true);

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
            => DbContextEventsPublisher.ExecuteSaveTransaction(this, () => base.SaveChanges(acceptAllChangesOnSuccess), _eventPublisher);

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => SaveChangesAsync(true, cancellationToken);

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
            => DbContextEventsPublisher.ExecuteSaveTransactionAsync(this, () => base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken), _eventPublisher);
    }
}
