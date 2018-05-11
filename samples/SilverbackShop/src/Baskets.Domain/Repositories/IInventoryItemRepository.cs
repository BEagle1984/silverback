using Common.Domain;
using Silverback.Infrastructure;
using SilverbackShop.Baskets.Domain.Model;

namespace SilverbackShop.Baskets.Domain.Repositories
{
    public interface IInventoryItemRepository : IAggregateRepository<InventoryItem>
    {
    }
}