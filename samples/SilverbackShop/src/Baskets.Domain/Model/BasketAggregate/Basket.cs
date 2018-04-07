using System;
using System.Collections.Generic;
using System.Linq;
using Common.Domain;
using Silverback.Domain;

namespace Baskets.Domain.Model.BasketAggregate
{
    public class Basket : ShopEntity, IAggregateRoot
    {
        private readonly List<BasketItem> _items = new List<BasketItem>();

        public IEnumerable<BasketItem> Items => _items.AsReadOnly();

        public Guid UserId { get; private set; }

        public DateTime Created { get; private set; }

        public DateTime? CheckoutDate { get; private set; }

        public static Basket Create(Guid userId)
        {
            return new Basket
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Created = DateTime.UtcNow
            };
        }

        public void Add(string productId, string name, int quantity, decimal unitPrice)
        {
            if (string.IsNullOrEmpty(productId)) throw new ArgumentNullException(nameof(productId));
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (quantity <= 0) throw new ArgumentException("quantity must be greater than 0", nameof(quantity));
            if (unitPrice <= 0) throw new ArgumentException("unitPrice must be greater than 0", nameof(unitPrice));

            var item = _items.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
            {
                item.Quantity += quantity;
                return;
            }

            _items.Add(new BasketItem
            {
                ProductId = productId,
                Name = name,
                Quantity = quantity,
                UnitPrice = unitPrice
            });
        }

        public void UpdateQuantity(string productId, int newQuantity)
        {
            if (string.IsNullOrEmpty(productId)) throw new ArgumentNullException(nameof(productId));
            if (newQuantity <= 0) throw new ArgumentException("newQuantity must be greater than 0", nameof(newQuantity));

            var item = _items.FirstOrDefault(i => i.ProductId == productId)
                       ?? throw new InvalidOperationException($"No item found with ProductId '{productId}'.");

            item.Quantity = newQuantity;
        }

        public void Remove(string productId)
        {
            if (string.IsNullOrEmpty(productId)) throw new ArgumentNullException(nameof(productId));

            var item = _items.FirstOrDefault(i => i.ProductId == productId)
                       ?? throw new InvalidOperationException($"No item found with ProductId '{productId}'.");

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
