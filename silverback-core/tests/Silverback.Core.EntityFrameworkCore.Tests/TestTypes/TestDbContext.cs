using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Silverback.Domain;
using Silverback.EntityFrameworkCore;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace Silverback.Core.EntityFrameworkCore.Tests.TestTypes
{
    public class TestDbContext : DbContext
    {
        private readonly IEventPublisher<IEvent> _eventPublisher;
        public DbSet<TestAggregateRoot> TestAggregates { get; set; }

        public TestDbContext(IEventPublisher<IEvent> eventPublisher)
        {
            _eventPublisher = eventPublisher;
        }

        public TestDbContext(DbContextOptions options, IEventPublisher<IEvent> eventPublisher)
            : base(options)
        {
            _eventPublisher = eventPublisher;
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
