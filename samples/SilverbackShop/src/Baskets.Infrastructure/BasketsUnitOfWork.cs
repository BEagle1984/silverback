using SilverbackShop.Baskets.Domain.Repositories;
using SilverbackShop.Common.Infrastructure;

namespace SilverbackShop.Baskets.Infrastructure
{
    public class BasketsUnitOfWork : UnitOfWork<BasketsContext>, IBasketsUnitOfWork
    {
        public BasketsUnitOfWork(BasketsContext dbContext)
            : base(dbContext)
        {
            Baskets = new BasketsRepository(dbContext.Baskets, this);
            InventoryItems = new InventoryItemsRepository(dbContext.InventoryItems, this);
        }

        public IBasketsRepository Baskets { get; }
        public IInventoryItemsRepository InventoryItems { get; }
    }
}