using System;
using System.Linq;
using Baskets.Domain.Model;
using Baskets.Domain.Repositories;

namespace Baskets.Domain.Services
{
    public class InventoryService
    {
        private readonly IInventoryItemRepository _repository;

        public InventoryService(IInventoryItemRepository repository)
        {
            _repository = repository;
        }

        public bool CheckIsInStock(string productId, int quantity)
        {
            var inventory = _repository.NoTrackingQueryable.FirstOrDefault(i => i.ProductId == productId);

            return inventory != null && inventory.StockQuantity >= quantity;
        }

        public void UpdateStock(string productId, int quantity)
        {
            var inventory = _repository.Queryable.FirstOrDefault(i => i.ProductId == productId);

            if (inventory != null)
            {
                inventory.UpdateStock(quantity);
                return;
            }

            if (quantity < 0)
                throw new InvalidOperationException($"No stock information found for product '{productId}'.");

            _repository.Add(InventoryItem.Create(productId, quantity));
        }
    }
}
