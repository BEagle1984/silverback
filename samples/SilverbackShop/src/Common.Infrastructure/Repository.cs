using Common.Domain;
using Common.Domain.Model;
using Common.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Silverback.Domain;

namespace SilverbackShop.Common.Infrastructure
{
    public abstract class Repository<TEntity> : IRepository<TEntity>
        where TEntity : ShopEntity, IAggregateRoot
    {
        protected DbSet<TEntity> DbSet;

        protected Repository(DbContext dbContext)
        {
            UnitOfWork = new UnitOfWork(dbContext);
            DbSet = dbContext.Set<TEntity>();
        }

        /// <summary>
        /// Adds the specified entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        public TEntity Add(TEntity entity) =>
            entity.IsTransient()
                ? DbSet.Add(entity).Entity
                : entity;

        /// <summary>
        /// Updates the specified entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        public TEntity Update(TEntity entity) => DbSet.Update(entity).Entity;

        /// <summary>
        /// Removes the specified entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        public void Remove(TEntity entity) => DbSet.Remove(entity);

        /// <summary>
        /// Gets the unit of work instance.
        /// </summary>
        public IUnitOfWork UnitOfWork { get; }
    }
}