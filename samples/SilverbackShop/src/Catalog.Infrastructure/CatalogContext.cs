using Microsoft.EntityFrameworkCore;
using Silverback.Core.EntityFrameworkCore;
using Silverback.Domain;
using Silverback.Messaging.Publishing;
using SilverbackShop.Catalog.Domain.Model;

namespace SilverbackShop.Catalog.Infrastructure
{
    public class CatalogContext : SilverbackDbContext
    {
        public DbSet<Product> Products { get; set; }

        public CatalogContext(IEventPublisher<IDomainEvent<IDomainEntity>> eventPublisher) 
            : base(eventPublisher)
        {
        }

        public CatalogContext(DbContextOptions options, IEventPublisher<IDomainEvent<IDomainEntity>> eventPublisher) 
            : base(options, eventPublisher)
        {
        }
    }
}
