using System;
using Common.Domain;
using Silverback.Domain;

namespace Baskets.Domain.Model
{
    public class InventoryItem : ShopEntity, IAggregateRoot
    {
        public string ProductId { get; private set; }

        public int StockQuantity { get; private set; }

        public static InventoryItem Create(string productId, int quantity)
        {
            return new InventoryItem
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                StockQuantity = quantity
            };
        }

        public void UpdateStock(int delta)
        {
            StockQuantity += delta;
        }
    }
}
