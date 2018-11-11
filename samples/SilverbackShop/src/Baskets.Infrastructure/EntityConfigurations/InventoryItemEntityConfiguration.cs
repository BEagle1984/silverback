using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SilverbackShop.Baskets.Domain.Model;

namespace SilverbackShop.Baskets.Infrastructure.EntityConfigurations
{
    public class InventoryItemEntityConfiguration : IEntityTypeConfiguration<InventoryItem>
    {
        public void Configure(EntityTypeBuilder<InventoryItem> builder)
        {
            builder.HasIndex(i => i.SKU).IsUnique();


        }
    }
}