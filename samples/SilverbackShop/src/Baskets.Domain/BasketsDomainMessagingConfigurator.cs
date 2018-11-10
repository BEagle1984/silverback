using System.Linq;
using Silverback.Messaging;
using Silverback.Messaging.Adapters;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using SilverbackShop.Baskets.Domain.Events;
using SilverbackShop.Baskets.Integration.Dto;

namespace SilverbackShop.Baskets.Domain
{
    public class BasketsDomainMessagingConfigurator : IConfigurator
    {
        public void Configure(BusConfig config)
        {
            // Translators
            config
                .AddTranslator<BasketCheckoutEvent, Integration.Events.BasketCheckoutEvent>(m =>
                    new Integration.Events.BasketCheckoutEvent
                    {
                        Items = m.Source.Items.Select(i =>
                            new BasketItemDto
                            {
                                Name = i.Name,
                                SKU = i.SKU,
                                Quantity = i.Quantity,
                                UnitPrice = i.UnitPrice
                            }).ToList()
                    });

            // Adapters
            config
                .AddOutbound<IIntegrationEvent, SimpleOutboundAdapter>(BasicEndpoint.Create("basket-events"))
                .AddInbound(BasicEndpoint.Create("catalog-events"));
        }
    }
}
