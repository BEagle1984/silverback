using SilverbackShop.Common.Infrastructure;

namespace SilverbackShop.Catalog.Domain.Repositories
{
    public interface ICatalogUnitOfWork : IUnitOfWork
    {
        IProductsRepository Products { get; }
    }
}