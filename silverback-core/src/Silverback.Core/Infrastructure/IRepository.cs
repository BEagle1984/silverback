using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silverback.Domain;

namespace Silverback.Infrastructure
{
    /// <summary>
    /// A repository.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    public interface IRepository<TEntity>
            where TEntity : IDomainEntity
    {
        /// <summary>
        /// Gets an <see cref="IQueryable" /> to query this repository.
        /// </summary>
        IQueryable<TEntity> Queryable { get; }

        /// <summary>
        /// Gets an <see cref="IQueryable" /> to query this repository. The loaded entities will not be tracked or cached.
        /// </summary>
        IQueryable<TEntity> NoTrackingQueryable { get; }

        /// <summary>
        /// Adds the specified entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        TEntity Add(TEntity entity);

        /// <summary>
        /// Updates the specified entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        TEntity Update(TEntity entity);

        /// <summary>
        /// Removes the specified entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        void Remove(TEntity entity);

        /// <summary>
        /// Gets the unit of work instance.
        /// </summary>
        IUnitOfWork UnitOfWork { get; }
    }
}
