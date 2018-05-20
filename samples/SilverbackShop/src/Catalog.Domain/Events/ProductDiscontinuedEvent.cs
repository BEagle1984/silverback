using Silverback.Domain;
using SilverbackShop.Catalog.Domain.Model;

namespace SilverbackShop.Catalog.Domain.Events
{
    public class ProductDiscontinuedEvent : DomainEvent<Product>
    {
    }
}