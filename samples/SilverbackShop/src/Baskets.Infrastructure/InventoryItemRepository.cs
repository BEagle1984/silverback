using System.Linq;
using Silverback.Core.EntityFrameworkCore;
using SilverbackShop.Baskets.Domain.Model;
using SilverbackShop.Baskets.Domain.Repositories;

namespace SilverbackShop.Baskets.Infrastructure
{
    public class InventoryItemRepository : AggregateRepository<InventoryItem>, IInventoryItemRepository
    {
        public InventoryItemRepository(BasketsContext context)
            : base(context.InventoryItems, context)
        {
        }

        public override IQueryable<InventoryItem> AggregateQueryable
            => DbSet;
    }
}