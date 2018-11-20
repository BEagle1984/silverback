using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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