using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Domain.Services;
using Silverback.Messaging.Subscribers;
using SilverbackShop.Baskets.Domain.Events;
using SilverbackShop.Baskets.Domain.Model;
using SilverbackShop.Baskets.Domain.Repositories;

namespace SilverbackShop.Baskets.Domain.Services
{
    public class InventoryService : IDomainService, ISubscriber
    {
        private readonly IInventoryItemsRepository _repository;

        public InventoryService(IInventoryItemsRepository repository)
        {
            _repository = repository;
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

        [Subscribe]
        public async Task OnCheckout(BasketCheckoutEvent message)
        {
            foreach (var item in message.Source.Items)
            {
                await DecrementStock(item.SKU, item.Quantity);
            }
        }
    }
}
