namespace Silverback.Integration.EntityFrameworkCore
{
    using System.Collections.Generic;

    namespace Silverback.Integration.EntityFrameworkCore
    {
        /// <summary>
        /// The repository to access the outbound queue.
        /// </summary>
        public interface IOutboundRepository<TEntity>
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
}
