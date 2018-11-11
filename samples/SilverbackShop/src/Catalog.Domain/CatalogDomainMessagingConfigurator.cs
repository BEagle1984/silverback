using System.Linq;
using Common.Domain.Services;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using SilverbackShop.Catalog.Domain.Events;
using SilverbackShop.Catalog.Domain.Model;
using SilverbackShop.Catalog.Integration.Dto;

namespace SilverbackShop.Catalog.Domain
{
    public class CatalogDomainMessagingConfigurator : IConfigurator
    {
        public void Configure(IBus bus)
        {
            bus
                // Translators
                .AddTranslator<ProductPublishedEvent, Integration.Events.ProductPublishedEvent>(m =>
                    new Integration.Events.ProductPublishedEvent {Product = MapProductDto(m.Source)})
                .AddTranslator<ProductUpdatedEvent, Integration.Events.ProductUpdatedEvent>(m =>
                    new Integration.Events.ProductUpdatedEvent {Product = MapProductDto(m.Source)})
                .AddTranslator<ProductDiscontinuedEvent, Integration.Events.ProductDiscontinuedEvent>(m =>
                    new Integration.Events.ProductDiscontinuedEvent {SKU = m.Source.SKU})
                // Subscribers
                .Subscribe<IDomainService>()
                // Connectors
                .AddOutbound<IIntegrationEvent>(BasicEndpoint.Create("catalog-events"));
        }

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
