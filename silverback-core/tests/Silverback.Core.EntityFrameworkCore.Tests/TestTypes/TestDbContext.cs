using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Silverback.Domain;
using Silverback.EntityFrameworkCore;
using Silverback.Messaging.Publishing;

namespace Silverback.Core.EntityFrameworkCore.Tests.TestTypes
{
    public class TestDbContext : DbContext
    {
        private readonly IEventPublisher<IDomainEvent<IDomainEntity>> _eventPublisher;
        public DbSet<TestAggregateRoot> TestAggregates { get; set; }

        public TestDbContext(IEventPublisher<IDomainEvent<IDomainEntity>> eventPublisher)
        {
            _eventPublisher = eventPublisher;
        }

        public TestDbContext(DbContextOptions options, IEventPublisher<IDomainEvent<IDomainEntity>> eventPublisher) 
            : base(options)
        {
            _eventPublisher = eventPublisher;
        }

        public override int SaveChanges() => this.SaveChanges(_eventPublisher);

        public override int SaveChanges(bool acceptAllChangesOnSuccess) =>
            this.SaveChanges(_eventPublisher, acceptAllChangesOnSuccess);

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            this.SaveChangesAsync(_eventPublisher, cancellationToken: cancellationToken);

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
            => this.SaveChangesAsync(_eventPublisher, acceptAllChangesOnSuccess, cancellationToken);
    }
}
