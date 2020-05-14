// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Silverback.Database
{
    /// <summary>
    ///     Abstracts the <c>DbSet</c> functionality to allow for multiple and decoupled implementations.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity being stored in this set.</typeparam>
    public interface IDbSet<TEntity>
        where TEntity : class
    {
        /// <summary>
        ///     Adds the specified entity to the set and start tracking it. The entity will be inserted into the
        ///     database when saving changes.
        /// </summary>
        /// <param name="entity">The entity to be added.</param>
        /// <returns>The added entity.</returns>
        TEntity Add(TEntity entity);

        /// <summary>
        ///     Removes the specified entity from the set causing it to be deleted when saving changes.
        /// </summary>
        /// <param name="entity">The entity to be removed.</param>
        /// <returns>The removed entity.</returns>
        TEntity Remove(TEntity entity);

        /// <summary>
        ///     Removes the specified entities from the set causing them to be deleted when saving changes.
        /// </summary>
        /// <param name="entities">The entities to be removed.</param>
        void RemoveRange(IEnumerable<TEntity> entities);

        /// <summary>
        ///     Finds the entity with the specified key(s). Returns <c>null</c> if not found.
        /// </summary>
        /// <param name="keyValues">The entity keys.</param>
        /// <returns>The entity found, or <c>null</c>.</returns>
        TEntity Find(params object[] keyValues);

        /// <summary>
        ///     Finds the entity with the specified key(s). Returns <c>null</c> if not found.
        /// </summary>
        /// <param name="keyValues">The entity keys.</param>
        /// <returns>The entity found, or <c>null</c>.</returns>
        Task<TEntity> FindAsync(params object[] keyValues);

        /// <summary>
        ///     Returns an <see cref="IQueryable"/> to query the set.
        /// </summary>
        /// <returns>An <see cref="IQueryable{T}"/>.</returns>
        IQueryable<TEntity> AsQueryable();

        /// <summary>
        ///     Returns the locally cached entities.
        /// </summary>
        /// <returns>The entities in the local cache.</returns>
        IEnumerable<TEntity> GetLocalCache();
    }
}