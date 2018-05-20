using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Common.Domain;
using Silverback.Domain;

namespace SilverbackShop.Baskets.Domain.Model
{
    public class Product : ShopEntity, IAggregateRoot
    {
        public static Product Create(string SKU, decimal unitPrice)
            => new Product
            {
                SKU = SKU,
                UnitPrice = unitPrice
            };

        [Required, MaxLength(100)]
        public string SKU { get; private set; }

        public decimal UnitPrice { get; private set; }

        public void UpdatePrice(decimal unitPrice)
        {
            UnitPrice = unitPrice;
        }
    }
}
