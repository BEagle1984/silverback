using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SilverbackShop.Baskets.Domain.Model;
using SilverbackShop.Baskets.Domain.Repositories;
using SilverbackShop.Common.Infrastructure;

namespace SilverbackShop.Baskets.Infrastructure
{
    public class InventoryItemsRepository : Repository<InventoryItem>, IInventoryItemsRepository
    {
        public InventoryItemsRepository(DbSet<InventoryItem> dbSet, IUnitOfWork unitOfWork)
            : base(dbSet, unitOfWork)
        {
        }

        public Task<InventoryItem> FindInventoryItemAsync(string productId)
            => DbSet.FirstOrDefaultAsync(i => i.ProductId == productId);

        public Task<int> GetStockQuantityAsync(string productId)
            => DbSet
                .Where(i => i.ProductId == productId)
                .Select(i => i.StockQuantity)
                .FirstOrDefaultAsync();
    }
}