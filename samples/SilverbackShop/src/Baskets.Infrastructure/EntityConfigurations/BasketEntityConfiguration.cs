using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SilverbackShop.Baskets.Domain.Model;

namespace SilverbackShop.Baskets.Infrastructure.EntityConfigurations
{
    public class BasketEntityConfiguration : IEntityTypeConfiguration<Basket>
    {
        public void Configure(EntityTypeBuilder<Basket> builder)
        {
            builder.Metadata
                .FindNavigation(nameof(Basket.Items))
                .SetPropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}