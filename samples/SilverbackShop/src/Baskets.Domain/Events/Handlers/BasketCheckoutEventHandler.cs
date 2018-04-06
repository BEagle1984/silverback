using Baskets.Domain.Services;
using Silverback.Messaging;

namespace Baskets.Domain.Events.Handlers
{
    public class BasketCheckoutEventHandler : MessageHandler<BasketCheckoutEvent>
    {
        private readonly InventoryService _inventoryService;

        public BasketCheckoutEventHandler(InventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        public override void Handle(BasketCheckoutEvent message)
        {
            foreach (var item in message.Source.Items)
            {
                _inventoryService.UpdateStock(item.ProductId, -item.Quantity);
            }
        }
    }
}
