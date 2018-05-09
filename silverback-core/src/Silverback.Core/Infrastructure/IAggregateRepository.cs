using System;
using System.Linq;
using Silverback.Domain;

namespace Silverback.Infrastructure
{
    /// <summary>
    /// A repository suitable for the <see cref="IAggregateRoot"/> entities.
    /// </summary>
    /// <typeparam name="TAggregateRoot">The type of the aggregate root entity.</typeparam>
    /// <seealso cref="Silverback.Infrastructure.IRepository{TAggregateRoot}" />
    public interface IAggregateRepository<TAggregateRoot> : IRepository<TAggregateRoot>
        where TAggregateRoot : IAggregateRoot
    {
        /// <summary>
        /// Gets the <see cref="IQueryable"/> that includes all entities related to the <see cref="IAggregateRoot"/> by default.
        /// </summary>
        IQueryable<TAggregateRoot> AggregateQueryable { get; }

        /// <summary>
        /// Gets an <see cref="IQueryable" /> that includes all entities related to the <see cref="IAggregateRoot"/> by defaul. The loaded entities will not be tracked or cached.
        /// </summary>
        IQueryable<TAggregateRoot> NoTrackingAggregateQueryable { get; }
    }
}