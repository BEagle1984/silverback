// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Silverback.Core.EntityFrameworkCore.Tests.TestTypes.Base.Domain;
using Silverback.EntityFrameworkCore;
using Silverback.Messaging.Publishing;

namespace Silverback.Core.EntityFrameworkCore.Tests.TestTypes
{
    public class TestDbContext : DbContext
    {
        private DbContextEventsPublisher<DomainEntity> _eventsPublisher;

        public DbSet<TestAggregateRoot> TestAggregates { get; set; }

        public TestDbContext(IPublisher publisher)
        {
            InitEventsPublisher(publisher);
        }

        public TestDbContext(DbContextOptions options, IPublisher publisher)
            : base(options)
        {
            InitEventsPublisher(publisher);
        }

        private void InitEventsPublisher(IPublisher publisher)
        {
            _eventsPublisher = new DbContextEventsPublisher<DomainEntity>(
                DomainEntityEventsAccessor.EventsSelector,
                DomainEntityEventsAccessor.ClearEventsAction,
                publisher,
                this);
        }

        public override int SaveChanges()
            => SaveChanges(true);

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
            => _eventsPublisher.ExecuteSaveTransaction(() => base.SaveChanges(acceptAllChangesOnSuccess));

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => SaveChangesAsync(true, cancellationToken);

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
            => _eventsPublisher.ExecuteSaveTransactionAsync(() =>
                base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken));
    }
}
