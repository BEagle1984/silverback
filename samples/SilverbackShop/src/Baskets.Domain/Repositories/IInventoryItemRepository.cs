using Common.Domain;
using SilverbackShop.Baskets.Domain.Model;

namespace SilverbackShop.Baskets.Domain.Repositories
{
    public interface IInventoryItemRepository : IShopRepository<InventoryItem>
    {
    }
}