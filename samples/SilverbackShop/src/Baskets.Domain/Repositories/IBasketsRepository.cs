using System;
using Silverback.Infrastructure;
using SilverbackShop.Baskets.Domain.Model;

namespace SilverbackShop.Baskets.Domain.Repositories
{
    public interface IBasketsRepository : IAggregateRepository<Basket>
    {
    }
}
