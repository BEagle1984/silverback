// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Connectors.Repositories.Model;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Connectors.Repositories
{
    /// <summary>
    ///     An outbound queue persisted in memory. Note that writing in the queue is thread-safe but reading is
    ///     not. Implements both <see cref="IOutboundQueueWriter" /> and <see cref="IOutboundQueueReader" />.
    /// </summary>
    [SuppressMessage("ReSharper", "CA1711", Justification = "Queue is just the right suffix in this case.")]
    public class InMemoryOutboundQueue
        : TransactionalList<QueuedMessage>, IOutboundQueueWriter, IOutboundQueueReader
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryOutboundQueue"/> class.
        /// </summary>
        /// <param name="sharedItems">
        ///     The messages shared between the instances of this repository.
        /// </param>
        public InMemoryOutboundQueue(TransactionalListSharedItems<QueuedMessage> sharedItems)
            : base(sharedItems)
        {
        }

        /// <inheritdoc />
        public Task Enqueue(IOutboundEnvelope envelope)
        {
            Check.NotNull(envelope, nameof(envelope));

            if (envelope.RawMessage == null)
            {
                envelope.RawMessage =
                    envelope.Endpoint.Serializer.Serialize(
                        envelope.Message,
                        envelope.Headers,
                        new MessageSerializationContext(envelope.Endpoint));
            }

            Add(new QueuedMessage(envelope.RawMessage, envelope.Headers, envelope.Endpoint));

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<int> GetLength() => Task.FromResult(CommittedItemsCount);

        /// <inheritdoc />
        public Task<TimeSpan> GetMaxAge() => Task.FromResult(TimeSpan.Zero);

        /// <inheritdoc />
        public Task<IReadOnlyCollection<QueuedMessage>> Dequeue(int count) =>
            Task.FromResult((IReadOnlyCollection<QueuedMessage>)Items.Take(count).Select(item => item.Item).ToList());

        /// <inheritdoc />
        public Task Retry(QueuedMessage queuedMessage)
        {
            // Nothing to do in the current implementation
            // --> the messages just stay in the queue until acknowledged
            // --> that's why reading is not thread-safe
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task Acknowledge(QueuedMessage queuedMessage)
        {
            Remove(queuedMessage);
            return Task.CompletedTask;
        }
    }
}
