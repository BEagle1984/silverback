using System.Collections.Generic;

namespace Silverback.Domain
{
    /// <summary>
    /// Exposes the methods to retrieve the <see cref="IDomainEvent{T}"/> collection related to 
    /// an entity.
    /// <see cref="Entity"/> already implements this interface
    /// </summary>
    public interface IDomainEntity
    {
        /// <summary>
        /// Gets the domain events published by this entity.
        /// </summary>
        /// <remarks></remarks>
        IEnumerable<IDomainEvent<IDomainEntity>> GetDomainEvents();

        /// <summary>
        /// Clears all domain events.
        /// </summary>
        void ClearEvents();
    }
}