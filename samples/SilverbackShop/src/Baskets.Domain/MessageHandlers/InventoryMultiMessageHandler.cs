using Silverback.Messaging;
using SilverbackShop.Baskets.Domain.Model.BasketAggregate;
using SilverbackShop.Baskets.Domain.Services;

namespace SilverbackShop.Baskets.Domain.MessageHandlers
{
    public class InventoryMultiMessageHandler : MultiMessageHandler
    {
        private readonly InventoryService _inventoryService;
        
        public InventoryMultiMessageHandler(InventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        protected override void Configure(MultiMessageHandlerConfiguration config) => config
            .AddHandler<BasketCheckoutEvent>(OnCheckout);

        public void OnCheckout(BasketCheckoutEvent message)
        {
            foreach (var item in message.Source.Items)
            {
                _inventoryService.UpdateStock(item.ProductId, -item.Quantity);
            }
        }
    }
}
