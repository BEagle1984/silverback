using System.Threading;
using System.Threading.Tasks;
using Common.Domain;
using Common.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace SilverbackShop.Common.Infrastructure
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DbContext _dbContext;

        public UnitOfWork(DbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
            => _dbContext.SaveChangesAsync(cancellationToken);
    }
}