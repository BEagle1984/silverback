// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Connectors.Repositories
{
    /// <summary>
    ///     An outbound queue persisted in memory. Note that writing in the queue is thread-safe but
    ///     reading is not.
    /// </summary>
    /// <seealso cref="IOutboundQueueProducer" />
    /// <seealso cref="IOutboundQueueConsumer" />
    public class InMemoryOutboundQueue
        : TransactionalList<QueuedMessage>,
            IOutboundQueueProducer,
            IOutboundQueueConsumer
    {
        #region Writer

        public Task Enqueue(IOutboundEnvelope envelope)
        {
            if (envelope.RawMessage == null)
                ((OutboundEnvelope) envelope).RawMessage =
                    envelope.Endpoint.Serializer.Serialize(
                        envelope.Message,
                        envelope.Headers,
                        new MessageSerializationContext(envelope.Endpoint));

            Add(new QueuedMessage(envelope.RawMessage, envelope.Headers, envelope.Endpoint));
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

        public Task<TimeSpan> GetMaxAge() => Task.FromResult(TimeSpan.Zero);

        public Task<IReadOnlyCollection<QueuedMessage>> Dequeue(int count) =>
            Task.FromResult((IReadOnlyCollection<QueuedMessage>) Entries.Take(count).ToList());

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