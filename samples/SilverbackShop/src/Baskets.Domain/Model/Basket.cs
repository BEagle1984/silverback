using System;
using System.Collections.Generic;
using System.Linq;
using Common.Domain;
using Common.Domain.Model;
using Silverback.Domain;
using SilverbackShop.Baskets.Domain.Events;

namespace SilverbackShop.Baskets.Domain.Model
{
    public class Basket : ShopEntity, IAggregateRoot
    {
        private readonly List<BasketItem> _items = new List<BasketItem>();

        public IEnumerable<BasketItem> Items => _items.AsReadOnly();

        public Guid UserId { get; private set; }

        public DateTime Created { get; private set; }

        public DateTime? CheckoutDate { get; private set; }

        private Basket()
        {
        }

        public static Basket Create(Guid userId)
        {
            return new Basket
            {
                UserId = userId,
                Created = DateTime.UtcNow
            };
        }

        public void Add(Product product, int quantity)
        {
            if (product == null) throw new ArgumentNullException(nameof(product));
            if (quantity <= 0) throw new ArgumentException("quantity must be greater than 0", nameof(quantity));

            var item = _items.FirstOrDefault(i => i.SKU == product.SKU);
            if (item != null)
            {
                item.Quantity += quantity;
                return;
            }

            _items.Add(new BasketItem
            {
                SKU = product.SKU,
                Name = product.DisplayName,
                Quantity = quantity,
                UnitPrice = product.UnitPrice
            });
        }

        public void UpdateQuantity(string sku, int newQuantity)
        {
            if (string.IsNullOrEmpty(sku)) throw new ArgumentNullException(nameof(sku));
            if (newQuantity <= 0) throw new ArgumentException("newQuantity must be greater than 0", nameof(newQuantity));

            var item = _items.FirstOrDefault(i => i.SKU == sku)
                       ?? throw new InvalidOperationException($"No item found with SKU '{sku}'.");

            item.Quantity = newQuantity;
        }

        public void Remove(string sku)
        {
            if (string.IsNullOrEmpty(sku)) throw new ArgumentNullException(nameof(sku));

            var item = _items.FirstOrDefault(i => i.SKU == sku)
                       ?? throw new InvalidOperationException($"No item found with SKU '{sku}'.");

            _items.Remove(item);
        }

        public void Clear()
        {
            _items.Clear();
        }

        public void Checkout()
        {
            CheckoutDate = DateTime.UtcNow;

            AddEvent<BasketCheckoutEvent>();
        }
    }
}
