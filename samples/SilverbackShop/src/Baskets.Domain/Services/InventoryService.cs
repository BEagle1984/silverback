using System;
using System.Linq;
using System.Threading.Tasks;
using SilverbackShop.Baskets.Domain.Model;
using SilverbackShop.Baskets.Domain.Repositories;

namespace SilverbackShop.Baskets.Domain.Services
{
    public class InventoryService
    {
        private readonly IInventoryItemsRepository _repository;

        public InventoryService(IInventoryItemsRepository repository)
        {
            _repository = repository;
        }

        public async Task<bool> CheckIsInStock(string productId, int quantity)
        {
            var stockQuantity = await _repository.GetStockQuantityAsync(productId);

            return stockQuantity >= quantity;
        }

        public async Task DecrementStock(string productId, int quantity)
        {
            var inventory = await _repository.FindInventoryItemAsync(productId);

            if (inventory == null)
                throw new InvalidOperationException($"No stock information found for product '{productId}'.");

            inventory.DecrementStock(quantity);
        }
    }
}
