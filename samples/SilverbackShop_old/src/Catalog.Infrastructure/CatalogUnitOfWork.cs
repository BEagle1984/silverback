using SilverbackShop.Catalog.Domain.Repositories;
using SilverbackShop.Common.Infrastructure;

namespace SilverbackShop.Catalog.Infrastructure
{
    public class CatalogUnitOfWork : UnitOfWork<CatalogContext>, ICatalogUnitOfWork
    {
        public CatalogUnitOfWork(CatalogContext dbContext)
            : base(dbContext)
        {
            Products = new ProductsRepository(dbContext.Products, this);
        }

        public IProductsRepository Products { get; }
    }
}