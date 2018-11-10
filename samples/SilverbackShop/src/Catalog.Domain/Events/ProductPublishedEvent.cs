using Silverback.Domain;
using SilverbackShop.Catalog.Domain.Model;

namespace SilverbackShop.Catalog.Domain.Events
{
    public class ProductPublishedEvent : DomainEvent<Product>
    {
    }
}