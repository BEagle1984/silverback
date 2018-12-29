using System.Linq;
using Common.Domain.Services;
using Silverback.Messaging;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using SilverbackShop.Catalog.Domain.Events;
using SilverbackShop.Catalog.Domain.Model;
using SilverbackShop.Catalog.Integration.Dto;

namespace SilverbackShop.Catalog.Domain.Services
{
    public class ProductEventsMapper : ISubscriber
    {
        [Subscribe]
        public IIntegrationMessage OnPublished(ProductPublishedEvent message) =>
            new Integration.Events.ProductPublishedEvent { Product = MapProductDto(message.Source) };

        [Subscribe]
        public IIntegrationMessage OnUpdate(ProductUpdatedEvent message) =>
            message.Source.Status == ProductStatus.Published
                ? new Integration.Events.ProductUpdatedEvent {Product = MapProductDto(message.Source)}
                : null;

        [Subscribe]
        public IIntegrationMessage OnDiscontinued(ProductDiscontinuedEvent message) =>
            new Integration.Events.ProductDiscontinuedEvent { SKU = message.Source.SKU };

        private static ProductDto MapProductDto(Product m)
            => new ProductDto
            {
                SKU = m.SKU,
                DisplayName = m.DisplayName,
                Description = m.Description,
                UnitPrice = m.UnitPrice
            };
    }
}
