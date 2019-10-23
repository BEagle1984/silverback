// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Silverback.Messaging.Connectors.Repositories
{
    public interface IOutboundQueueConsumer
    {
        Task<int> GetLength();

        Task<TimeSpan> GetMaxAge();

        Task<IEnumerable<QueuedMessage>> Dequeue(int count);

        /// <summary>
        /// Re-enqueue the message to retry.
        /// </summary>
        Task Retry(QueuedMessage queuedMessage);

        /// <summary>
        /// Acknowledges the specified message has been sent.
        /// </summary>
        Task Acknowledge(QueuedMessage queuedMessage);
    }
}