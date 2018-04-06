using System.Linq;
using Baskets.Domain.Model;
using Baskets.Domain.Repositories;
using Common.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Silverback.Infrastructure;

namespace Baskets.Infrastructure
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