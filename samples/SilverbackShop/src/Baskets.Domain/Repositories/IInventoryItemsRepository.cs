using System.Threading.Tasks;
using Common.Domain;
using Common.Domain.Repositories;
using SilverbackShop.Baskets.Domain.Model;
using SilverbackShop.Common.Infrastructure;

namespace SilverbackShop.Baskets.Domain.Repositories
{
    public interface IInventoryItemsRepository : IRepository<InventoryItem>
    {
        Task<InventoryItem> FindInventoryItemAsync(string sku);

        Task<int> GetStockQuantityAsync(string sku);
    }
}