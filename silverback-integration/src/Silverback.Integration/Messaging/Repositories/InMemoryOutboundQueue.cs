using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Repositories
{
    /// <summary>
    /// An outbound queue persisted in memory. Note that writing in the queue is thread-safe but
    /// reading is not.
    /// </summary>
    /// <seealso cref="IOutboundQueueWriter" />
    /// <seealso cref="IOutboundQueueReader" />
    public class InMemoryOutboundQueue : TransactionalList<QueuedMessage>, IOutboundQueueWriter, IOutboundQueueReader
    {
        #region Writer

        public void Enqueue(IIntegrationMessage message, IEndpoint endpoint)
            => Add(new QueuedMessage(message, endpoint));

        #endregion

        #region Reader

        public IEnumerable<QueuedMessage> Dequeue(int count)
            => Entries.Take(count);

        public void Retry(QueuedMessage message)
        {
            // Nothing to do in the current implementation
            // --> the messages just stay in the queue until acknowledged
            // --> that's why reading is not thread-safe
        }

        public void Acknowledge(QueuedMessage message)
            => Remove(message);

        #endregion
    }
}