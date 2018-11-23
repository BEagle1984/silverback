using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Common.Domain;
using Common.Domain.Repositories;
using SilverbackShop.Baskets.Domain.Model;

namespace SilverbackShop.Baskets.Domain.Repositories
{
    public interface IProductsRepository : IRepository<Product>
    {
        Task<Product> FindBySkuAsync(string sku);
    }
}
