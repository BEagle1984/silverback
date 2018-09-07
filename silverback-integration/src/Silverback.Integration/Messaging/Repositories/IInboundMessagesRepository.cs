using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Repositories
{
    /// <summary>
    /// The repository to access the inbox table.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    public interface IInboundMessagesRepository<TEntity>
        where TEntity : IInboundMessageEntity
    {
        /// <summary>
        /// Creates a new entity.
        /// </summary>
        /// <returns></returns>
        TEntity Create();

        /// <summary>
        /// Adds the specified entity to the inbox.
        /// </summary>
        /// <param name="entity">The entity.</param>
        void Add(TEntity entity);

        /// <summary>
        /// Returns a boolean value indicating whether a message with the specified Id is already
        /// stored in the inbox table.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="endpoint">The name of the source endpoint.</param>
        /// <returns></returns>
        bool Exists(Guid messageId, string endpoint);

        /// <summary>
        /// Saves the changes made to the tracked entities.
        /// </summary>
        void SaveChanges();
    }
}