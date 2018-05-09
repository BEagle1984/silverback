using System.Linq;
using Microsoft.EntityFrameworkCore;
using Silverback.Domain;
using Silverback.Infrastructure;

namespace Silverback.Core.EntityFrameworkCore
{
    /// <summary>
    /// The base class for the repositories that handle the <see cref="IAggregateRoot"/>.
    /// </summary>
    /// <typeparam name="TAggregateRoot">The type of the aggregate root.</typeparam>
    /// <seealso cref="Silverback.Core.EntityFrameworkCore.Repository{TAggregateRoot}" />
    /// <seealso cref="Silverback.Infrastructure.IAggregateRepository{TAggregateRoot}" />
    public abstract class AggregateRepository<TAggregateRoot> : Repository<TAggregateRoot>, IAggregateRepository<TAggregateRoot>
        where TAggregateRoot : class, IDomainEntity, IAggregateRoot
    {
        /// <summary>
        /// Gets the <see cref="T:System.Linq.IQueryable" /> that includes all entities related to the <see cref="T:Silverback.Domain.IAggregateRoot" /> by default.
        /// </summary>
        public abstract IQueryable<TAggregateRoot> AggregateQueryable { get; }

        /// <summary>
        /// Gets an <see cref="T:System.Linq.IQueryable" /> that includes all entities related to the <see cref="T:Silverback.Domain.IAggregateRoot" /> by defaul. The loaded entities will not be tracked or cached.
        /// </summary>
        public IQueryable<TAggregateRoot> NoTrackingAggregateQueryable => AggregateQueryable.AsNoTracking();

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateRepository{TAggregateRoot}"/> class.
        /// </summary>
        /// <param name="dbSet">The database set.</param>
        /// <param name="unitOfWork">The unit of work.</param>
        protected AggregateRepository(DbSet<TAggregateRoot> dbSet, IUnitOfWork unitOfWork)
            : base(dbSet, unitOfWork)
        {
        }
    }
}