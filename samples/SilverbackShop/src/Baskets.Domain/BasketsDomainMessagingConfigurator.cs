using System.Linq;
using Common.Domain.Services;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using SilverbackShop.Baskets.Domain.Events;
using SilverbackShop.Baskets.Integration.Dto;

namespace SilverbackShop.Baskets.Domain
{
    public class BasketsDomainMessagingConfigurator : IConfigurator
    {
        public void Configure(IBus bus)
        {
            bus
                // Translators
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
                    })
                // Subscribers
                .Subscribe<IDomainService>()
                // Connectors
                .AddOutbound<IIntegrationEvent>(BasicEndpoint.Create("basket-events"))
                .AddInbound(BasicEndpoint.Create("catalog-events"));
        }
    }
}
