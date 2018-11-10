using Common.Domain;
using Microsoft.EntityFrameworkCore;
using Silverback.Domain;

namespace SilverbackShop.Common.Infrastructure
{
    /// <summary>
    /// The base class for the repositories.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <seealso cref="IRepository{TEntity}" />
    public abstract class Repository<TEntity> : IRepository<TEntity>
        where TEntity : ShopEntity, IAggregateRoot
    {
        protected DbSet<TEntity> DbSet { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Repository{TEntity}" /> class.
        /// </summary>
        /// <param name="dbSet">The database set.</param>
        /// <param name="unitOfWork">The unit of work.</param>
        protected Repository(DbSet<TEntity> dbSet, IUnitOfWork unitOfWork)
        {
            DbSet = dbSet;
            UnitOfWork = unitOfWork;
        }

        /// <summary>
        /// Adds the specified entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        public TEntity Add(TEntity entity)
            => DbSet.Add(entity).Entity;

        /// <summary>
        /// Updates the specified entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        public TEntity Update(TEntity entity)
            => DbSet.Update(entity).Entity;

        /// <summary>
        /// Removes the specified entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        public void Remove(TEntity entity)
            => DbSet.Remove(entity);

        /// <summary>
        /// Gets the unit of work instance.
        /// </summary>
        public IUnitOfWork UnitOfWork { get; }
    }
}