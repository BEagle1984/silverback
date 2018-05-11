using System.Linq;
using Silverback.Messaging;
using Silverback.Messaging.Adapters;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using SilverbackShop.Baskets.Domain.Events;
using SilverbackShop.Baskets.Domain.MessageHandlers;

namespace SilverbackShop.Baskets.Domain
{
    public class BasketsDomainMessagingConfigurator : IConfigurator
    {
        public void Configure(BusConfig config)
        {
            config
                // Translators
                .AddTranslator<BasketCheckoutEvent, Integration.Events.BasketCheckoutEvent>(m =>
                    new Integration.Events.BasketCheckoutEvent
                    {
                        Items = m.Source.Items.Select(i =>
                            new Integration.BasketItem
                            {
                                Name = i.Name,
                                ProductId = i.ProductId,
                                Quantity = i.Quantity,
                                UnitPrice = i.UnitPrice
                            }).ToList()
                    })
                // Message Handlers
                .Subscribe<InventoryMultiMessageHandler>()
                // Outbound Adapters
                .AddOutbound<IIntegrationEvent, SimpleOutboundAdapter>(BasicEndpoint.Create("basket-events"));
        }
    }
}
