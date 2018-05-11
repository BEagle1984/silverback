using System.Threading.Tasks;
using Silverback.Infrastructure;
using SilverbackShop.Catalog.Domain.Model;

namespace SilverbackShop.Catalog.Infrastructure
{
    public interface IProductsRepository : IAggregateRepository<Product>
    {
        Task<Product> FindBySkuAsync(string sku);
    }
}