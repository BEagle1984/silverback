using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SilverbackShop.Baskets.Domain.Model;
using SilverbackShop.Baskets.Domain.Repositories;

namespace SilverbackShop.Baskets.Domain.Services
{
    public class CheckoutService
    {
        private readonly IBasketsRepository _repository;
        private readonly InventoryService _inventoryService;

        public CheckoutService(IBasketsUnitOfWork unitOfWork, InventoryService inventoryService)
        {
            _repository = unitOfWork.Baskets;
            _inventoryService = inventoryService;
        }

        public async Task Checkout(Basket basket)
        {
            await ValidateBasket(basket);

            basket.Checkout();

            await _repository.UnitOfWork.SaveChangesAsync();
        }

        private async Task ValidateBasket(Basket basket)
        {
            if (basket == null || !basket.Items.Any())
                throw new BasketValidationException("The basket is empty.");

            var inventoryErrors = new List<string>();

            foreach (var item in basket.Items)
            {
                if (!await _inventoryService.CheckIsInStock(item.SKU, item.Quantity))
                {
                    inventoryErrors.Add($"The product '{item.SKU}' is not available in the desired quantity.");
                }
            }

            if (inventoryErrors.Any())
            {
                throw new BasketValidationException(string.Join(" ", inventoryErrors.ToArray()));
            }
        }
    }
}
