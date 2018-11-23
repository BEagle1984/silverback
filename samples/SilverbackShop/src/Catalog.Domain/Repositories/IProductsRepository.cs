using System.Threading.Tasks;
using Common.Domain.Repositories;
using SilverbackShop.Catalog.Domain.Model;

namespace SilverbackShop.Catalog.Domain.Repositories
{
    public interface IProductsRepository : IRepository<Product>
    {
        Task<Product> FindBySkuAsync(string sku);
    }
}