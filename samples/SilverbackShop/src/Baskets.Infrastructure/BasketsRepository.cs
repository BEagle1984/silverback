using System;
using System.Linq;
using Baskets.Domain.Model.BasketAggregate;
using Baskets.Domain.Repositories;
using Common.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Silverback.Infrastructure;

namespace Baskets.Infrastructure
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