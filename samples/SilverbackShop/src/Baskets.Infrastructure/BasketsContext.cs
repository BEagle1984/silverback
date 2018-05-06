using Microsoft.EntityFrameworkCore;
using Silverback.Core.EntityFrameworkCore;
using Silverback.Domain;
using Silverback.Messaging.Publishing;
using SilverbackShop.Baskets.Domain.Model;

namespace SilverbackShop.Baskets.Infrastructure
{
    public class BasketsContext : SilverbackDbContext
    {
        public DbSet<Basket> Baskets { get; set; }
        public DbSet<BasketItem> BasketItems { get; set; }
        public DbSet<InventoryItem> InventoryItems { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BasketsContext"/> class.
        /// </summary>
        /// <param name="eventPublisher">The event publisher.</param>
        public BasketsContext(IEventPublisher<IDomainEvent<IDomainEntity>> eventPublisher) 
            : base(eventPublisher)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BasketsContext"/> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="eventPublisher">The event publisher.</param>
        public BasketsContext(DbContextOptions options, IEventPublisher<IDomainEvent<IDomainEntity>> eventPublisher)
            : base(options, eventPublisher)
        {
        }

        /// <summary>
        /// Override this method to further configure the model that was discovered by convention from the entity types
        /// exposed in <see cref="T:Microsoft.EntityFrameworkCore.DbSet`1" /> properties on your derived context. The resulting model may be cached
        /// and re-used for subsequent instances of your derived context.
        /// </summary>
        /// <param name="modelBuilder">The builder being used to construct the model for this context. Databases (and other extensions) typically
        /// define extension methods on this object that allow you to configure aspects of the model that are specific
        /// to a given database.</param>
        /// <remarks>
        /// If a model is explicitly set on the options for this context (via <see cref="M:Microsoft.EntityFrameworkCore.DbContextOptionsBuilder.UseModel(Microsoft.EntityFrameworkCore.Metadata.IModel)" />)
        /// then this method will not be run.
        /// </remarks>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<InventoryItem>().HasIndex(i => i.ProductId).IsUnique();

            modelBuilder
                .Entity<Basket>().Metadata
                .FindNavigation(nameof(Basket.Items))
                .SetPropertyAccessMode(PropertyAccessMode.Field);

            base.OnModelCreating(modelBuilder);
        }
    }
}
