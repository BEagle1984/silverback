using Common.Domain;

namespace SilverbackShop.Catalog.Domain.Repositories
{
    public interface ICatalogUnitOfWork : IUnitOfWork
    {
        IProductsRepository Products { get; }
    }
}