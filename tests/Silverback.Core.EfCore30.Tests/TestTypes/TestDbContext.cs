// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Silverback.EntityFrameworkCore;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Core.EFCore30.TestTypes.Model;

// ReSharper disable once CheckNamespace
namespace Silverback.Tests.Core.EFCore30.TestTypes
{
    public class TestDbContext : DbContext
    {
        private readonly DbContextEventsPublisher _eventsPublisher;

        public TestDbContext(IPublisher publisher)
        {
            _eventsPublisher = new DbContextEventsPublisher(publisher, this);
        }

        public TestDbContext(DbContextOptions options, IPublisher publisher)
            : base(options)
        {
            _eventsPublisher = new DbContextEventsPublisher(publisher, this);
        }

        public DbSet<TestAggregateRoot> TestAggregates { get; set; } = null!;

        public DbSet<Person> Persons { get; set; } = null!;

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