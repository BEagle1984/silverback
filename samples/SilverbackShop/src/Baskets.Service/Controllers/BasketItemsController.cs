using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SilverbackShop.Baskets.Domain.Model.BasketAggregate;
using SilverbackShop.Baskets.Domain.Repositories;
using SilverbackShop.Baskets.Domain.Services;
using SilverbackShop.Baskets.Service.Dto;
using SilverbackShop.Common.Data;

namespace SilverbackShop.Baskets.Service.Controllers
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
        public async Task<ActionResult> Post([FromBody]AddBasketItemDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

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

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            var ex = context?.Exception;

            if (ex is ArgumentException argEx)
            {
                context.Result = new ObjectResult(new { Error = argEx.Message }) { StatusCode = (int)HttpStatusCode.BadRequest };
            }

        }
    }
}