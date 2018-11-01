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
    /// <seealso cref="IDisposable" />
    public class InMemoryOutboundQueue : IOutboundQueueWriter, IOutboundQueueReader
    {
        private static readonly List<QueuedMessage> Queue = new List<QueuedMessage>();
        private readonly List<QueuedMessage> _uncommittedQueue = new List<QueuedMessage>();

        #region Writer

        public void Enqueue(IIntegrationMessage message, IEndpoint endpoint)
        {
            lock (_uncommittedQueue)
            {
                _uncommittedQueue.Add(new QueuedMessage(message, endpoint));
            }
        }

        public void Commit()
        {
            lock (_uncommittedQueue)
            {
                lock (Queue)
                {
                    Queue.AddRange(_uncommittedQueue);
                }

                _uncommittedQueue.Clear();
            }
        }

        public void Rollback()
        {
            lock (_uncommittedQueue)
            {
                _uncommittedQueue.Clear();
            }
        }

        #endregion

        #region Reader

        public int Length => Queue.Count;

        public IEnumerable<QueuedMessage> Dequeue(int count)
        {
            lock (Queue)
            {
                return Queue.Take(count);
            }
        }

        public void Retry(QueuedMessage message)
        {
            // Nothing to do in the current implementation
            // --> the messages just stay in the queue until acknowledged
            // --> that's why reading is not thread-safe
        }

        public void Acknowledge(QueuedMessage message)
        {
            lock (Queue)
            {
                Queue.Remove(message);
            }
        }

        #endregion

        public void Clear()
        {
            lock (Queue)
            {
                Queue.Clear();
            }
        }
    }
}