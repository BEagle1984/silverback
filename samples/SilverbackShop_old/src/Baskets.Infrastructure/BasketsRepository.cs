using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Domain;
using Microsoft.EntityFrameworkCore;
using SilverbackShop.Baskets.Domain.Model;
using SilverbackShop.Baskets.Domain.Repositories;
using SilverbackShop.Common.Infrastructure;

namespace SilverbackShop.Baskets.Infrastructure
{
    public class BasketsRepository : Repository<Basket>, IBasketsRepository
    {
        public BasketsRepository(DbSet<Basket> dbSet, IUnitOfWork unitOfWork) 
            : base(dbSet, unitOfWork)
        {
        }

        public Task<Basket> FindByUserAsync(Guid userId)
            => DbSet.Include(b => b.Items).FirstOrDefaultAsync(b => b.UserId == userId);
    }
}