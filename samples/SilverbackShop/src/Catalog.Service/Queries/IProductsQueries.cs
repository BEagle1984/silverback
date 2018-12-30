using System.Threading.Tasks;
using SilverbackShop.Catalog.Service.Dto;

namespace SilverbackShop.Catalog.Service.Queries
{
    public interface IProductsQueries
    {
        Task<ProductDto[]> GetAllAsync(bool includeDiscontinued = false);
        Task<ProductDto[]> GetAllDiscontinuedAsync();
    }
}