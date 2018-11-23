using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Common.Domain;
using Microsoft.EntityFrameworkCore;
using SilverbackShop.Baskets.Domain.Model;
using SilverbackShop.Baskets.Domain.Repositories;
using SilverbackShop.Common.Infrastructure;
using SilverbackShop.Common.Infrastructure.Data;

namespace SilverbackShop.Baskets.Infrastructure
{
    public class ProductsRepository : Repository<Product>, IProductsRepository
    {
        public ProductsRepository(BasketsDbContext dbContext) : base(dbContext)
        {
        }

        public Task<Product> FindBySkuAsync(string sku)
            => DbSet.FirstOrDefaultAsync(p => p.SKU == sku);
    }
}
