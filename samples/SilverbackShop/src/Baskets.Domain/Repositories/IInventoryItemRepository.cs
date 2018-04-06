using Baskets.Domain.Model;
using Common.Domain;

namespace Baskets.Domain.Repositories
{
    public interface IInventoryItemRepository : IShopRepository<InventoryItem>
    {
    }
}