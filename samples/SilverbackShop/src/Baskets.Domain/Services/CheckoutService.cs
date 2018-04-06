using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Baskets.Domain.Model.BasketAggregate;
using Baskets.Domain.Repositories;

namespace Baskets.Domain.Services
{
    public class CheckoutService
    {
        private readonly IBasketsRepository _repository;
        private readonly InventoryService _inventoryService;

        public CheckoutService(IBasketsRepository repository, InventoryService inventoryService)
        {
            _repository = repository;
            _inventoryService = inventoryService;
        }

        public Task Checkout(Basket basket)
        {
            ValidateBasket(basket);

            basket.Checkout();

            return _repository.UnitOfWork.SaveChangesAsync();
        }

        private void ValidateBasket(Basket basket)
        {
            if (basket == null || !basket.Items.Any())
                throw new BasketValidationException("The basket is empty.");

            var inventoryErrors = new List<string>();

            foreach (var item in basket.Items)
            {
                if (!_inventoryService.CheckIsInStock(item.ProductId, item.Quantity))
                {
                    inventoryErrors.Add($"The product {item.ProductId} is not available in the desired quantity.");
                }
            }

            if (inventoryErrors.Any())
            {
                throw new BasketValidationException(string.Join(" ", inventoryErrors.ToArray()));
            }
        }
    }
}
