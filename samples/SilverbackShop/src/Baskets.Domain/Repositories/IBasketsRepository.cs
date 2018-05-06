using System;
using Common.Domain;
using SilverbackShop.Baskets.Domain.Model;

namespace SilverbackShop.Baskets.Domain.Repositories
{
    public interface IBasketsRepository : IShopRepository<Basket>
    {
        Basket FindUserBasket(Guid userId);

        Basket GetUserBasket(Guid userId);
    }
}
