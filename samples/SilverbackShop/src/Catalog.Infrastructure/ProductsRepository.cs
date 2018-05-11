using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Silverback.Core.EntityFrameworkCore;
using SilverbackShop.Catalog.Domain.Model;

namespace SilverbackShop.Catalog.Infrastructure
{
    public class ProductsRepository : AggregateRepository<Product>, IProductsRepository
    {
        public ProductsRepository(CatalogContext context)
            : base(context.Products, context)
        {
        }

        public override IQueryable<Product> AggregateQueryable => Queryable;
        public Task<Product> FindBySkuAsync(string sku)
            => AggregateQueryable.FirstOrDefaultAsync(p => p.SKU == sku);
    }
}
