using System.Collections.Generic;
using System.Threading.Tasks;

namespace Silverback.Messaging.Connectors.Repositories
{
    public interface IOutboundQueueReader
    {
        int Length { get; }

        IEnumerable<QueuedMessage> Dequeue(int count);

        /// <summary>
        /// Re-enqueue the message to retry.
        /// </summary>
        void Retry(QueuedMessage message);

        /// <summary>
        /// Acknowledges the specified message has been sent.
        /// </summary>
        void Acknowledge(QueuedMessage message);
    }
}