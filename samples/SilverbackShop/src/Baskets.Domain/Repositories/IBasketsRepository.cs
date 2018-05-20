using System;
using System.Threading.Tasks;
using Common.Domain;
using SilverbackShop.Baskets.Domain.Model;
using SilverbackShop.Common.Infrastructure;

namespace SilverbackShop.Baskets.Domain.Repositories
{
    public interface IBasketsRepository : IRepository<Basket>
    {
        Task<Basket> FindByUserAsync(Guid userId);
    }
}
