using System.Threading.Tasks;
using Silverback.Messaging;
using Silverback.Messaging.Subscribers;
using SilverbackShop.Baskets.Domain.Events;
using SilverbackShop.Baskets.Domain.Services;

namespace SilverbackShop.Baskets.Domain.MessageHandlers
{
    public class InventoryMultiSubscriber : MultiSubscriber
    {
        private readonly InventoryService _inventoryService;
        
        public InventoryMultiSubscriber(InventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        protected override void Configure(MultiSubscriberConfig config) => config
            .AddAsyncHandler<BasketCheckoutEvent>(OnCheckout);

        public async Task OnCheckout(BasketCheckoutEvent message)
        {
            foreach (var item in message.Source.Items)
            {
                await _inventoryService.DecrementStock(item.ProductId, item.Quantity);
            }
        }
    }
}
