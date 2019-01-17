// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Silverback.EntityFrameworkCore;
using Silverback.Messaging.Publishing;

namespace Silverback.Core.EntityFrameworkCore.Tests.TestTypes
{
    public class TestDbContext : DbContext
    {
        private readonly IPublisher _publisher;
        public DbSet<TestAggregateRoot> TestAggregates { get; set; }

        public TestDbContext(IPublisher eventPublisher)
        {
            _publisher = eventPublisher;
        }

        public TestDbContext(DbContextOptions options, IPublisher publisher)
            : base(options)
        {
            _publisher = publisher;
        }

        public override int SaveChanges()
            => SaveChanges(true);

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
            => DbContextEventsPublisher.ExecuteSaveTransaction(this, () => base.SaveChanges(acceptAllChangesOnSuccess), _publisher);

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => SaveChangesAsync(true, cancellationToken);

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
            => DbContextEventsPublisher.ExecuteSaveTransactionAsync(this, () => base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken), _publisher);
    }
}
