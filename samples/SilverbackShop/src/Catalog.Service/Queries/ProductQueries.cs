using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SilverbackShop.Catalog.Domain.Model;
using SilverbackShop.Catalog.Infrastructure;
using SilverbackShop.Catalog.Service.Dto;
using SilverbackShop.Common.Infrastructure.Data;

namespace SilverbackShop.Catalog.Service.Queries
{
    public class ProductQueries : Queries<Product>, IProductQueries
    {
        public ProductQueries(CatalogDbContext dbContext) : base(dbContext)
        {
        }

        public Task<ProductDto[]> GetAllAsync(bool includeDiscontinued = false)
            => Query
                .Where(p => includeDiscontinued || p.Status != ProductStatus.Discontinued)
                .Select(p => Map(p))
                .ToArrayAsync();

        public Task<ProductDto[]> GetAllDiscontinuedAsync()
            => Query
                .Where(p => p.Status == ProductStatus.Discontinued)
                .Select(p => Map(p))
                .ToArrayAsync();

        private ProductDto Map(Product product)
            => new ProductDto
            {
                SKU = product.SKU,
                DisplayName = product.DisplayName,
                UnitPrice = product.UnitPrice,
                Description = product.Description,
                IsPublished = product.Status == ProductStatus.Published,
                IsDiscontinued = product.Status == ProductStatus.Discontinued
            };
    }
}
