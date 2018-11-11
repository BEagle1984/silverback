using System;
using Common.Domain;
using Common.Domain.Model;
using Silverback.Domain;

namespace SilverbackShop.Baskets.Domain.Model
{
    public class InventoryItem : ShopEntity, IAggregateRoot
    {
        public string SKU { get; private set; }

        public int StockQuantity { get; private set; }

        /// <summary>
        /// Creates the specified sku.
        /// </summary>
        /// <param name="sku">The sku.</param>
        /// <param name="quantity">The quantity.</param>
        /// <returns></returns>
        public static InventoryItem Create(string sku, int quantity)
        {
            return new InventoryItem
            {
                SKU = sku,
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
