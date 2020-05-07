// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Connectors.Repositories.Model;

namespace Silverback.Messaging.Connectors.Repositories
{
    /// <summary>
    ///     Exposes the methods to read from the outbound queue. Used by the <see cref="IOutboundQueueWorker" />
    ///     .
    /// </summary>
    public interface IOutboundQueueReader
    {
        /// <summary>
        ///     Returns the total number of messages in the queue.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task{TResult}" /> representing the result of the asynchronous operation. The task
        ///     result contains the queue length.
        /// </returns>
        Task<int> GetLength();

        /// <summary>
        ///     Gets a <see cref="TimeSpan" /> representing the time elapsed since the oldest message currently in
        ///     the queue was written.
        /// </summary>
        /// <returns> The age of the oldest message. </returns>
        Task<TimeSpan> GetMaxAge();

        /// <summary>
        ///     Pulls the specified number of items from the queue (according to the FIFO rule). The operation must
        ///     be acknowledged for items to be really removed from the queue.
        /// </summary>
        /// <param name="count"> The number of items to be dequeued. </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the result of the asynchronous operation. The task result
        ///     contains the collection of <see cref="QueuedMessage" />.
        /// </returns>
        Task<IReadOnlyCollection<QueuedMessage>> Dequeue(int count);

        /// <summary>
        ///     Called after the message has been successfully produced to remove it from the queue.
        /// </summary>
        /// <param name="queuedMessage"> The message that was processed. </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the result of the asynchronous operation.
        /// </returns>
        Task Acknowledge(QueuedMessage queuedMessage);

        /// <summary>
        ///     Called when an error occurs producing the message to re-enqueue it and retry later on.
        /// </summary>
        /// <param name="queuedMessage"> The message that was processed. </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the result of the asynchronous operation.
        /// </returns>
        Task Retry(QueuedMessage queuedMessage);
    }
}
