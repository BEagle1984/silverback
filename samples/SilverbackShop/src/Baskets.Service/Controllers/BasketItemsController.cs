using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SilverbackShop.Baskets.Domain.Model;
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
        private readonly IBasketsUnitOfWork _unitOfWork;

        public BasketItemsController(BasketsService basketService, IBasketsUnitOfWork unitOfWork)
        {
            _basketService = basketService;
            _unitOfWork = unitOfWork;
        }

        private Task<Basket> GetBasket()
            => _basketService.GetUserBasket(UserData.DefaultUserId);

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

            var product = await _unitOfWork.Products.FindBySkuAsync(dto.SKU);
            if (product == null)
                return BadRequest("Product not found.");

            var basket = await GetBasket();
            basket.Add(product, dto.Quantity);
            await _unitOfWork.SaveChangesAsync();
            return NoContent();
        }
        [HttpPatch("{sku}")]
        public async Task<ActionResult> Post(string sku, UpdateBasketItemDto dto)
        {
            var basket = await GetBasket();
            basket.UpdateQuantity(sku, dto.Quantity);
            await _unitOfWork.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{sku}")]
        public async Task<ActionResult> Delete(string sku)
        {
            var basket = await GetBasket();
            basket.Remove(sku);
            await _unitOfWork.SaveChangesAsync();
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