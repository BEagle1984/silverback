using System.Threading.Tasks;
using Baskets.Domain.Model.BasketAggregate;
using Baskets.Domain.Repositories;
using Baskets.Domain.Services;
using Baskets.Service.Dto;
using Common.Data;
using Microsoft.AspNetCore.Mvc;

namespace Baskets.Service.Controllers
{
    [Route("basket/items")]
    public class BasketItemsController : Controller
    {
        private readonly BasketsService _basketService;
        private readonly IBasketsRepository _repository;

        public BasketItemsController(IBasketsRepository repository, BasketsService basketService)
        {
            _repository = repository;
            _basketService = basketService;
        }

        private Task<Basket> GetBasket()
            => _basketService.GetBasket(UserData.DefaultUserId);

        [HttpGet("")]
        public async Task<ActionResult> Get()
        {
            var basket = await GetBasket();
            return Ok(basket.Items);
        }

        [HttpPost]
        public async Task<ActionResult> Post(AddBasketItemDto dto)
        {
            var basket = await GetBasket();
            basket.Add(dto.ProductId, dto.ProductName, dto.Quantity, dto.UnitPrice);
            await _repository.UnitOfWork.SaveChangesAsync();
            return NoContent();
        }
        [HttpPatch("{productId}")]
        public async Task<ActionResult> Post(string productId, UpdateBasketItemDto dto)
        {
            var basket = await GetBasket();
            basket.UpdateQuantity(productId, dto.Quantity);
            await _repository.UnitOfWork.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{productId}")]
        public async Task<ActionResult> Delete(string productId)
        {
            var basket = await GetBasket();
            basket.Remove(productId);
            await _repository.UnitOfWork.SaveChangesAsync();
            return NoContent();
        }
    }
}