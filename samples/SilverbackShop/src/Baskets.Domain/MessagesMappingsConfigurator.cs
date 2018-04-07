using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Baskets.Domain.Model.BasketAggregate;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;

namespace Baskets.Domain
{
    public static class MessagesMappingsConfigurator
    {
        public static void Configure(IBus bus)
        {
            bus.Config().AddTranslator<BasketCheckoutEvent, Integration.Events.BasketCheckoutEvent>(m =>
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
                });
        }
    }
}
