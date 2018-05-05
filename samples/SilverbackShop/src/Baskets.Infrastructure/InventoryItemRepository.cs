using System.Linq;
using SilverbackShop.Baskets.Domain.Model;
using SilverbackShop.Baskets.Domain.Repositories;
using SilverbackShop.Common.Infrastructure;

namespace SilverbackShop.Baskets.Infrastructure
{
    public class InventoryItemRepository : ShopRepository<InventoryItem>, IInventoryItemRepository
    {
        public InventoryItemRepository(BasketsContext context)
            : base(context.InventoryItems, context)
        {
        }

        public override IQueryable<InventoryItem> AggregateQueryable
            => DbSet;
    }
}