using System.Threading.Tasks;
using SilverbackShop.Catalog.Service.Dto;

namespace SilverbackShop.Catalog.Service.Queries
{
    public interface IProductQueries
    {
        Task<ProductDto[]> GetAllAsync(bool includeDiscontinued = false);
        Task<ProductDto[]> GetAllDiscontinuedAsync();
    }
}