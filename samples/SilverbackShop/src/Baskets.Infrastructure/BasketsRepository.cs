using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Silverback.Core.EntityFrameworkCore;
using SilverbackShop.Baskets.Domain.Model;
using SilverbackShop.Baskets.Domain.Repositories;

namespace SilverbackShop.Baskets.Infrastructure
{
    public class BasketsRepository : AggregateRepository<Basket>, IBasketsRepository
    {
        public BasketsRepository(BasketsContext context)
            : base(context.Baskets, context)
        {
        }

        public override IQueryable<Basket> AggregateQueryable
            => DbSet.Include(b => b.Items);
    }
}