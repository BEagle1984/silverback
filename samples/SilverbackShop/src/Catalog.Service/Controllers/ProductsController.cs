using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SilverbackShop.Catalog.Domain.Model;
using SilverbackShop.Catalog.Domain.Repositories;
using SilverbackShop.Catalog.Infrastructure;
using SilverbackShop.Catalog.Service.Dto;

namespace SilverbackShop.Catalog.Service.Controllers
{
    [Route("api/products")]
    public class ProductsController : Controller
    {
        private readonly IProductsRepository _repository;

        public ProductsController(IProductsRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            return Ok(await _repository.GetAllAsync());
        }

        [HttpGet("discontinued")]
        public async Task<IActionResult> GetDiscontinued()
        {
            return Ok(await _repository.GetAllDiscontinuedAsync());
        }

        [HttpGet("{sku}")]
        public async Task<IActionResult> Get(string sku)
        {
            return Ok(await _repository.FindBySkuAsync(sku));
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody]NewProductDto dto)
        {
            var product = Product.Create(dto.SKU, dto.DisplayName, dto.UnitPrice, dto.Description);

            _repository.Add(product);

            await _repository.UnitOfWork.SaveChangesAsync();

            return Ok(product);
        }

        [HttpPut("{sku}")]
        public async Task<IActionResult> Put(string sku, [FromBody]UpdateProductDto dto)
        {
            var product = await _repository.FindBySkuAsync(sku);

            product.Update(dto.DisplayName, dto.UnitPrice, dto.Description);

            await _repository.UnitOfWork.SaveChangesAsync();

            return Ok(product);
        }

        [HttpPost("{sku}/publish")]
        public async Task<IActionResult> Publish(string sku)
        {
            var product = await _repository.FindBySkuAsync(sku);

            if (product == null)
                return NotFound();

            product.Publish();

            await _repository.UnitOfWork.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("{sku}/discontinue")]
        public async Task<IActionResult> Discontinue(string sku)
        {
            var product = await _repository.FindBySkuAsync(sku);

            if (product == null)
                return NotFound();

            product.Discontinue();

            await _repository.UnitOfWork.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{sku}")]
        public async Task<IActionResult> Delete(string sku)
        {
            var product = await _repository.FindBySkuAsync(sku);

            if (product == null)
                return NotFound();

            // TODO: Find better pattern for this case. The logic should be in the model.
            if (product.Status == ProductStatus.Published)
                return BadRequest("Published products cannot be deleted.");
            
            _repository.Remove(product);

            await _repository.UnitOfWork.SaveChangesAsync();

            return NoContent();
        }
    }
}
