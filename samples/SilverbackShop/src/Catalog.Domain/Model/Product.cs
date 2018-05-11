using System;
using Common.Domain;
using Silverback.Domain;
using System.ComponentModel.DataAnnotations;
using SilverbackShop.Catalog.Domain.Events;

namespace SilverbackShop.Catalog.Domain.Model
{
    public class Product : ShopEntity, IAggregateRoot
    {
        [Required, MaxLength(100)]
        public string SKU { get; private set; }

        [Required, MaxLength(300)]
        public string DisplayName { get; private set; }

        [Required]
        public string Description { get; private set; }

        public decimal UnitPrice { get; private set; }

        public ProductStatus Status { get; private set; }

        public static Product Create(string sku, string displayName, decimal unitPrice, string description = null)
        {
            return new Product
            {
                Id = Guid.NewGuid(),
                Status = ProductStatus.New,
                SKU = sku,
                DisplayName = displayName,
                UnitPrice = unitPrice,
                Description = description ?? "- no description -"
            };
        }

        public Product Update(string displayName, decimal unitPrice, string description = null)
        {
            DisplayName = displayName;
            UnitPrice = unitPrice;
            Description = description ?? "- no description -";

            AddEvent<ProductUpdatedEvent>();

            return this;
        }
        public void Publish()
        {
            switch (Status)
            {
                case ProductStatus.Discontinued:
                case ProductStatus.New:
                    Status = ProductStatus.Published;
                    AddEvent<ProductPublishedEvent>();
                    break;
                case ProductStatus.Published:
                    throw new DomainValidationException("This product is already published.");
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Discontinue()
        {
            switch (Status)
            {
                case ProductStatus.Published:
                    Status = ProductStatus.Discontinued;
                    AddEvent<ProductDiscontinuedEvent>();
                    break;
                case ProductStatus.Discontinued:
                    throw new DomainValidationException("This product was already discontinued.");
                case ProductStatus.New:
                    throw new DomainValidationException("This product cannot be discontinued because it was never published.");
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
