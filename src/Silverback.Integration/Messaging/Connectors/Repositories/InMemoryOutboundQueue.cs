// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Connectors.Repositories
{
    /// <summary>
    ///     An outbound queue persisted in memory. Note that writing in the queue is thread-safe but
    ///     reading is not.
    ///     Implements both <see cref="IOutboundQueueProducer" /> and <see cref="IOutboundQueueConsumer" />.
    /// </summary>
    public class InMemoryOutboundQueue
        : TransactionalList<QueuedMessage>, IOutboundQueueProducer, IOutboundQueueConsumer
    {
        public InMemoryOutboundQueue(TransactionalListSharedItems<QueuedMessage> sharedItems)
            : base(sharedItems)
        {
        }

        #region Producer

        public Task Enqueue(IOutboundEnvelope envelope)
        {
            if (envelope.RawMessage == null)
                envelope.RawMessage =
                    envelope.Endpoint.Serializer.Serialize(
                        envelope.Message,
                        envelope.Headers,
                        new MessageSerializationContext(envelope.Endpoint));

            Add(new QueuedMessage(envelope.RawMessage, envelope.Headers, envelope.Endpoint));
            return Task.CompletedTask;
        }

        #endregion

        #region Consumer

        public Task<int> GetLength() => Task.FromResult(CommittedItemsCount);

        public Task<TimeSpan> GetMaxAge() => Task.FromResult(TimeSpan.Zero);

        public Task<IReadOnlyCollection<QueuedMessage>> Dequeue(int count) =>
            Task.FromResult((IReadOnlyCollection<QueuedMessage>) Items.Take(count).Select(item => item.Entry).ToList());

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