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

        public InventoryService(IBasketsUnitOfWork unitOfWork)
        {
            _repository = unitOfWork.InventoryItems;
        }

        public async Task<bool> CheckIsInStock(string sku, int quantity)
        {
            var stockQuantity = await _repository.GetStockQuantityAsync(sku);

            return stockQuantity >= quantity;
        }

        public async Task DecrementStock(string sku, int quantity)
        {
            var inventory = await _repository.FindInventoryItemAsync(sku);

            if (inventory == null)
                throw new InvalidOperationException($"No stock information found for product '{sku}'.");

            inventory.DecrementStock(quantity);
        }
    }
}
