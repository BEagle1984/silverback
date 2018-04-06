using System;
using Baskets.Domain.Model.BasketAggregate;
using Common.Domain;

namespace Baskets.Domain.Repositories
{
    public interface IBasketsRepository : IShopRepository<Basket>
    {
        Basket FindUserBasket(Guid userId);

        Basket GetUserBasket(Guid userId);
    }
}
