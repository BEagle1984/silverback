using System;
using System.Linq;
using Silverback.Domain;
using Silverback.Infrastructure;

namespace Common.Domain
{
    public interface IShopRepository<TAggregateRoot> : IRepository<TAggregateRoot>
        where TAggregateRoot : ShopEntity, IAggregateRoot
    {
        /// <summary>
        /// Gets the <see cref="IQueryable"/> that includes all entities related to the <see cref="IAggregateRoot"/> by default.
        /// </summary>
        /// <value>
        /// The aggregate queryable.
        /// </value>
        IQueryable<TAggregateRoot> AggregateQueryable { get; }

        TAggregateRoot GetById(Guid id);
    }
}
