using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SilverbackShop.Baskets.Domain;
using SilverbackShop.Baskets.Domain.Services;
using SilverbackShop.Common.Data;

namespace SilverbackShop.Baskets.Service.Controllers
{
    [Route("basket")]
    public class BasketsController : Controller
    {
        private readonly BasketsService _basketService;
        private readonly CheckoutService _checkoutService;

        public BasketsController(CheckoutService checkoutService, BasketsService basketService)
        {
            _checkoutService = checkoutService;
            _basketService = basketService;
        }

        [HttpGet]
        public async Task<ActionResult> Get()
        {
            return Ok(await _basketService.GetOrCreateBasket(UserData.DefaultUserId));
        }

        [HttpPost("checkout")]
        public async Task<ActionResult> Checkout()
        {
            var basket = await _basketService.GetOrCreateBasket(UserData.DefaultUserId);

            try
            {
                await _checkoutService.Checkout(basket);

                return NoContent();
            }
            catch (BasketValidationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
