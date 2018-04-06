using System.Collections.Generic;

namespace Silverback.Messaging.Repositories
{
    /// <summary>
    /// The repository to access the outbox table.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    public interface IOutboundMessagesRepository<TEntity>
        where TEntity : IOutboundMessageEntity
    {
        /// <summary>
        /// Creates a new entity.
        /// </summary>
        /// <returns></returns>
        TEntity Create();

        /// <summary>
        /// Adds the specified entity to the outbox.
        /// </summary>
        /// <param name="entity">The entity.</param>
        void Add(TEntity entity);

        /// <summary>
        /// Gets the pending messages that needs to be sent to the message broker.
        /// </summary>
        /// <returns></returns>
        IEnumerable<TEntity> GetPending();

        /// <summary>
        /// Saves the changes made to the tracked entities.
        /// </summary>
        void SaveChanges(); 
    }
}