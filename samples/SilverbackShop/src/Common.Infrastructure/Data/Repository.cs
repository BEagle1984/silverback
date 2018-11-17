using Common.Domain.Model;
using Common.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Silverback.Domain;

namespace SilverbackShop.Common.Infrastructure.Data
{
    public abstract class Repository<TEntity> : IRepository<TEntity>
        where TEntity : ShopEntity, IAggregateRoot
    {
        protected readonly DbSet<TEntity> DbSet;

        protected Repository(DbContext dbContext)
        {
            UnitOfWork = new UnitOfWork(dbContext);
            DbSet = dbContext.Set<TEntity>();
        }

        public TEntity Add(TEntity entity) =>
            entity.IsTransient()
                ? DbSet.Add(entity).Entity
                : entity;

        public TEntity Update(TEntity entity) => DbSet.Update(entity).Entity;

        public void Remove(TEntity entity) => DbSet.Remove(entity);

        public IUnitOfWork UnitOfWork { get; }
    }
}