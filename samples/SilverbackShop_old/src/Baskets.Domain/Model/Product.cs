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
        public static Product Create(string SKU, string displayName, decimal unitPrice)
            => new Product
            {
                SKU = SKU,
                DisplayName = displayName,
                UnitPrice = unitPrice
            };

        [Required, MaxLength(100)]
        public string SKU { get; private set; }

        [Required, MaxLength(300)]
        public string DisplayName { get; private set; }

        public decimal UnitPrice { get; private set; }

        public void Update(string displayName, decimal unitPrice)
        {
            DisplayName = displayName;
            UnitPrice = unitPrice;
        }
    }
}
