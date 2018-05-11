using System;
using Common.Domain;
using Silverback.Domain;

namespace SilverbackShop.Baskets.Domain.Model
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

        public void DecrementStock(int quantity)
        {
            if (quantity <= 0)
                throw new ArgumentOutOfRangeException();
            if (quantity >= StockQuantity)
                throw new DomainValidationException($"Cannot decrement by {quantity} units. Current stock quantity is {StockQuantity}.");

            StockQuantity -= quantity;
        }
        public void IncrementStock(int quantity)
        {
            if (quantity <= 0)
                throw new ArgumentOutOfRangeException();

            StockQuantity += quantity;
        }

        public void UpdateStock(int quantity)
        {
            if (quantity < 0)
                throw new ArgumentOutOfRangeException();

            StockQuantity = quantity;
        }
    }
}
