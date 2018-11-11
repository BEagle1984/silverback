using Microsoft.EntityFrameworkCore;
using Silverback.Domain;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using SilverbackShop.Baskets.Domain.Model;
using SilverbackShop.Baskets.Infrastructure.EntityConfigurations;
using SilverbackShop.Common.Infrastructure;

namespace SilverbackShop.Baskets.Infrastructure
{
    public class BasketsDbContext : ShopDbContext
    {
        public DbSet<Basket> Baskets { get; set; }
        public DbSet<BasketItem> BasketItems { get; set; }
        public DbSet<InventoryItem> InventoryItems { get; set; }
        public DbSet<Product> Products { get; set; }

        public BasketsDbContext(IEventPublisher<IEvent> eventPublisher) 
            : base(eventPublisher)
        {
        }

        public BasketsDbContext(DbContextOptions options, IEventPublisher<IEvent> eventPublisher)
            : base(options, eventPublisher)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new InventoryItemEntityConfiguration());
            modelBuilder.ApplyConfiguration(new BasketEntityConfiguration());

            base.OnModelCreating(modelBuilder);
        }
    }

    namespace EntityConfigurations
    {
    }
}
