using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Common.Domain;
using Microsoft.EntityFrameworkCore;
using SilverbackShop.Baskets.Domain.Model;
using SilverbackShop.Baskets.Domain.Repositories;
using SilverbackShop.Common.Infrastructure;

namespace SilverbackShop.Baskets.Infrastructure
{
    public class ProductsRepository : Repository<Product>, IProductsRepository
    {
        public ProductsRepository(DbSet<Product> dbSet, IUnitOfWork unitOfWork) 
            : base(dbSet, unitOfWork)
        {
        }

        public Task<Product> FindBySkuAsync(string sku)
            => DbSet.FirstOrDefaultAsync(p => p.SKU == sku);
    }
}
