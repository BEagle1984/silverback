using Common.Domain.Model;
using Silverback.Domain;

namespace Common.Domain.Repositories
{
    public interface IRepository<TEntity>
           where TEntity : ShopEntity, IAggregateRoot
    {
        TEntity Add(TEntity entity);

        TEntity Update(TEntity entity);

        void Remove(TEntity entity);

        IUnitOfWork UnitOfWork { get; }
    }
}