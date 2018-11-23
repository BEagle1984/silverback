using System.Linq;
using Common.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace SilverbackShop.Common.Infrastructure.Data
{
    public abstract class Queries<TEntity>
        where TEntity : ShopEntity
    {
        protected IQueryable<TEntity> Query;

        protected Queries(DbContext dbContext)
        {
            Query = dbContext.Set<TEntity>().AsNoTracking();
        }
    }
}