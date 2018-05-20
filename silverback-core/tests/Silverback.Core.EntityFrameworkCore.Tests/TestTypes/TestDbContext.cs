using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Silverback.Domain;
using Silverback.EntityFrameworkCore;
using Silverback.Messaging.Publishing;

namespace Silverback.Core.EntityFrameworkCore.Tests.TestTypes
{
    public class TestDbContext : SilverbackDbContext
    {
        public DbSet<TestAggregateRoot> TestAggregates { get; set; }
    
        public TestDbContext(IEventPublisher<IDomainEvent<IDomainEntity>> eventPublisher)
            : base(eventPublisher)
        {
        }

        public TestDbContext(DbContextOptions options, IEventPublisher<IDomainEvent<IDomainEntity>> eventPublisher) : base(options, eventPublisher)
        {
        }
    }
}
