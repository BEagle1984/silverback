using System.Linq;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using SilverbackShop.Baskets.Domain.Events;
using SilverbackShop.Baskets.Integration.Dto;

namespace SilverbackShop.Baskets.Domain.Services
{
    public class BasketEventsMapper : ISubscriber
    {
        [Subscribe]
        public IIntegrationMessage OnCheckout(BasketCheckoutEvent message) =>
            new Integration.Events.BasketCheckoutEvent
            {
                Items = message.Source.Items.Select(i =>
                    new BasketItemDto
                    {
                        Name = i.Name,
                        SKU = i.SKU,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice
                    }).ToList()
            };
    }
}