using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SilverbackShop.Catalog.Domain.Model;
using SilverbackShop.Catalog.Domain.Repositories;
using SilverbackShop.Common.Infrastructure.Data;

namespace SilverbackShop.Catalog.Infrastructure
{
    public class ProductsRepository : Repository<Product>, IProductsRepository
    {
        public ProductsRepository(DbContext dbContext) : base(dbContext)
        {
        }

        public Task<Product> FindBySkuAsync(string sku)
            => DbSet.FirstOrDefaultAsync(p => p.SKU == sku);
    }
}
