using System;
using System.Linq;
using Common.Domain;
using Microsoft.EntityFrameworkCore;
using Silverback.Core.EntityFrameworkCore;
using Silverback.Domain;
using Silverback.Infrastructure;

namespace Common.Infrastructure
{
    public abstract class ShopRepository<TAggregateRoot> : Repository<TAggregateRoot> , IShopRepository<TAggregateRoot>
        where TAggregateRoot : ShopEntity, IAggregateRoot
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ShopRepository{TAggregateRoot}" /> class.
        /// </summary>
        /// <param name="dbSet">The database set.</param>
        /// <param name="unitOfWork">The unit of work.</param>
        public ShopRepository(DbSet<TAggregateRoot> dbSet, IUnitOfWork unitOfWork)
            : base(dbSet, unitOfWork)
        {
        }

        /// <summary>
        /// Gets the <see cref="T:System.Linq.IQueryable" /> that includes all entities related to the <see cref="T:Silverback.Domain.IAggregateRoot" /> by default.
        /// </summary>
        /// <value>
        /// The aggregate queryable.
        /// </value>
        public abstract IQueryable<TAggregateRoot> AggregateQueryable { get; }

        public virtual TAggregateRoot GetById(Guid id)
            => AggregateQueryable.FirstOrDefault(x => x.Id == id) ?? throw new EntityNotFoundException();
    }
}