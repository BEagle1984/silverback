using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Baskets.Domain.MessageHandlers;
using Baskets.Domain.Model.BasketAggregate;
using Silverback.Messaging;
using Silverback.Messaging.Adapters;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;

namespace Baskets.Domain
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
