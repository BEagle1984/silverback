using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SilverbackShop.Baskets.Domain.Model;
using SilverbackShop.Baskets.Domain.Repositories;
using SilverbackShop.Common.Infrastructure;

namespace SilverbackShop.Baskets.Infrastructure
{
    public class BasketsRepository : ShopRepository<Basket>, IBasketsRepository
    {
        public BasketsRepository(BasketsContext context)
            : base(context.Baskets, context)
        {
        }

        public override IQueryable<Basket> AggregateQueryable
            => DbSet.Include(b => b.Items);

        public Basket FindUserBasket(Guid userId)
            => AggregateQueryable.FirstOrDefault(b => b.UserId == userId);

        public Basket GetUserBasket(Guid userId)
            => AggregateQueryable.FirstOrDefault(b => b.UserId == userId) ?? throw new EntityNotFoundException();

    }
}