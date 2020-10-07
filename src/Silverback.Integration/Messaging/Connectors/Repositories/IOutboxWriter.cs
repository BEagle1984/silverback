// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors.Repositories
{
    /// <summary>
    ///     Exposes the methods to write to the outbound queue. Used by the
    ///     <see cref="DeferredOutboundConnector" />.
    /// </summary>
    public interface IOutboxWriter
    {
        /// <summary>
        ///     Adds the message contained in the specified envelope to the outbound queue.
        /// </summary>
        /// <param name="envelope">
        ///     The envelope containing the message to be enqueued.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task WriteAsync(IOutboundEnvelope envelope);

        /// <summary>
        ///     Called to commit the transaction, storing the pending messages to the queue.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task CommitAsync();

        /// <summary>
        ///     Called to rollback the transaction, preventing the pending messages to be stored in the queue.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task RollbackAsync();
    }
}
