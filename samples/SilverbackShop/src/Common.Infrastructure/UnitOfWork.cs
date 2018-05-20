using System.Threading;
using System.Threading.Tasks;
using Common.Domain;
using Microsoft.EntityFrameworkCore;

namespace SilverbackShop.Common.Infrastructure
{
    public abstract class UnitOfWork<T> : IUnitOfWork
        where T : DbContext
    {
        private readonly T _dbContext;

        protected UnitOfWork(T dbContext)
        {
            _dbContext = dbContext;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
            => _dbContext.SaveChangesAsync(cancellationToken);
    }
}