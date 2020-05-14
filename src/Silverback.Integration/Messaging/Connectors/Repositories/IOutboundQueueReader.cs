// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Silverback.Messaging.Connectors.Repositories
{
    /// <summary>
    ///     Exposes the methods to read from the outbound queue. Used by the <see cref="IOutboundQueueWorker"/>.
    /// </summary>
    public interface IOutboundQueueReader
    {
        Task<int> GetLength();

        Task<TimeSpan> GetMaxAge();

        Task<IReadOnlyCollection<QueuedMessage>> Dequeue(int count);

        /// <summary>
        ///     Re-enqueue the message to retry.
        /// </summary>
        Task Retry(QueuedMessage queuedMessage);

        /// <summary>
        ///     Acknowledges the specified message has been sent.
        /// </summary>
        Task Acknowledge(QueuedMessage queuedMessage);
    }
}