// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors.Repositories
{
    /// <summary>
    /// An outbound queue persisted in memory. Note that writing in the queue is thread-safe but
    /// reading is not.
    /// </summary>
    /// <seealso cref="IOutboundQueueProducer" />
    /// <seealso cref="IOutboundQueueConsumer" />
    public class InMemoryOutboundQueue : TransactionalList<QueuedMessage>, IOutboundQueueProducer, IOutboundQueueConsumer
    {
        #region Writer

        public Task Enqueue(IOutboundMessage message)
        {
            Add(new QueuedMessage(message));
            return Task.CompletedTask;
        }

        public new Task Commit()
        {
            base.Commit();
            return Task.CompletedTask;
        }

        public new Task Rollback()
        {
            base.Rollback();
            return Task.CompletedTask;
        }

        #endregion

        #region Reader

        public Task<IEnumerable<QueuedMessage>> Dequeue(int count) => Task.FromResult(Entries.Take(count).ToArray().AsEnumerable());

        public Task Retry(QueuedMessage queuedMessage)
        {
            // Nothing to do in the current implementation
            // --> the messages just stay in the queue until acknowledged
            // --> that's why reading is not thread-safe
            return Task.CompletedTask;
        }

        public Task Acknowledge(QueuedMessage queuedMessage)
        {
            Remove(queuedMessage);
            return Task.CompletedTask;
        }

        #endregion
    }
}