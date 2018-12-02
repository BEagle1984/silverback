// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;

namespace Silverback.Messaging.Connectors.Repositories
{
    public interface IOutboundQueueConsumer
    {
        int Length { get; }

        IEnumerable<QueuedMessage> Dequeue(int count);

        /// <summary>
        /// Re-enqueue the message to retry.
        /// </summary>
        void Retry(QueuedMessage queuedMessage);

        /// <summary>
        /// Acknowledges the specified message has been sent.
        /// </summary>
        void Acknowledge(QueuedMessage queuedMessage);
    }
}