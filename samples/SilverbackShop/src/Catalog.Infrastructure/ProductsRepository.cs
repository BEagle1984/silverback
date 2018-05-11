using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Silverback.Core.EntityFrameworkCore;
using SilverbackShop.Catalog.Domain.Dto;
using SilverbackShop.Catalog.Domain.Model;
using SilverbackShop.Catalog.Domain.Repositories;
using SilverbackShop.Common.Infrastructure;

namespace SilverbackShop.Catalog.Infrastructure
{
    public class ProductsRepository : Repository<Product>, IProductsRepository
    {
        public ProductsRepository(DbSet<Product> dbSet, IUnitOfWork unitOfWork)
            : base(dbSet, unitOfWork)
        {
        }

        public Task<Product> FindBySkuAsync(string sku)
            => DbSet.FirstOrDefaultAsync(p => p.SKU == sku);

        public Task<ProductDto[]> GetAllAsync(bool includeDiscontinued = false)
            => DbSet
                .Where(p => includeDiscontinued || p.Status != ProductStatus.Discontinued)
                .Select(p => Map(p))
                .ToArrayAsync();
        
        public Task<ProductDto[]> GetAllDiscontinuedAsync()
            => DbSet
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
