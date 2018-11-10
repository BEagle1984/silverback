using System.Threading.Tasks;
using Common.Domain;
using SilverbackShop.Catalog.Domain.Dto;
using SilverbackShop.Catalog.Domain.Model;

namespace SilverbackShop.Catalog.Domain.Repositories
{
    public interface IProductsRepository : IRepository<Product>
    {
        Task<Product> FindBySkuAsync(string sku);

        Task<ProductDto[]> GetAllAsync(bool includeDiscontinued = false);

        Task<ProductDto[]> GetAllDiscontinuedAsync();
    }
}