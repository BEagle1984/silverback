// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.TransactionalOutbox.Repositories.Model;
using Silverback.Util;

namespace Silverback.Messaging.Outbound.TransactionalOutbox.Repositories
{
    /// <summary>
    ///     An outbound queue persisted in memory. Note that writing in the queue is thread-safe but reading is
    ///     not. Implements both <see cref="IOutboxWriter" /> and <see cref="IOutboxReader" />.
    /// </summary>
    // TODO: Deprecate? Move to tests?
    public class InMemoryOutbox : TransactionalList<OutboxStoredMessage>, IOutboxWriter, IOutboxReader
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="InMemoryOutbox" /> class.
        /// </summary>
        /// <param name="sharedItems">
        ///     The messages shared between the instances of this repository.
        /// </param>
        public InMemoryOutbox(TransactionalListSharedItems<OutboxStoredMessage> sharedItems)
            : base(sharedItems)
        {
        }

        /// <inheritdoc cref="IOutboxWriter.WriteAsync" />
        public Task WriteAsync(
            object? message,
            byte[]? messageBytes,
            IReadOnlyCollection<MessageHeader>? headers,
            string endpointName,
            string actualEndpointName) =>
            AddAsync(new OutboxStoredMessage(
                message?.GetType(),
                messageBytes,
                headers,
                endpointName,
                actualEndpointName));

        /// <inheritdoc cref="IOutboxReader.GetLengthAsync" />
        public Task<int> GetLengthAsync() => Task.FromResult(CommittedItemsCount);

        /// <inheritdoc cref="IOutboxReader.GetMaxAgeAsync" />
        public Task<TimeSpan> GetMaxAgeAsync() => Task.FromResult(TimeSpan.Zero);

        /// <inheritdoc cref="IOutboxReader.ReadAsync" />
        public Task<IReadOnlyCollection<OutboxStoredMessage>> ReadAsync(int count) =>
            Task.FromResult(
                (IReadOnlyCollection<OutboxStoredMessage>)Items.Take(count).Select(item => item.Item).ToList());

        /// <inheritdoc cref="IOutboxReader.AcknowledgeAsync(OutboxStoredMessage)" />
        public Task AcknowledgeAsync(OutboxStoredMessage outboxMessage) =>
            RemoveAsync(outboxMessage);

        /// <inheritdoc cref="IOutboxReader.AcknowledgeAsync(IEnumerable{OutboxStoredMessage})" />
        public Task AcknowledgeAsync(IEnumerable<OutboxStoredMessage> outboxMessages) =>
            outboxMessages.ForEachAsync(RemoveAsync);

        /// <inheritdoc cref="IOutboxReader.RetryAsync(OutboxStoredMessage)" />
        public Task RetryAsync(OutboxStoredMessage outboxMessage)
        {
            // Nothing to do in the current implementation
            // --> the messages just stay in the queue until acknowledged
            // --> that's why reading is not thread-safe
            return Task.CompletedTask;
        }

        /// <inheritdoc cref="IOutboxReader.RetryAsync(IEnumerable{OutboxStoredMessage})" />
        public Task RetryAsync(IEnumerable<OutboxStoredMessage> outboxMessages)
        {
            // Nothing to do in the current implementation
            // --> the messages just stay in the queue until acknowledged
            // --> that's why reading is not thread-safe
            return Task.CompletedTask;
        }
    }
}
