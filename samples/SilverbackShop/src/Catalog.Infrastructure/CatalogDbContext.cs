using Microsoft.EntityFrameworkCore;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using SilverbackShop.Catalog.Domain.Model;
using SilverbackShop.Common.Infrastructure.Data;

namespace SilverbackShop.Catalog.Infrastructure
{
    public class CatalogDbContext : ShopDbContext
    {
        public DbSet<Product> Products { get; set; }

        public CatalogDbContext(IEventPublisher<IEvent> eventPublisher) 
            : base(eventPublisher)
        {
        }

        public CatalogDbContext(DbContextOptions options, IEventPublisher<IEvent> eventPublisher) 
            : base(options, eventPublisher)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>().HasAlternateKey(p => p.SKU);

            base.OnModelCreating(modelBuilder);
        }
    }
}
