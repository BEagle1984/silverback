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
        public async Task WriteAsync(IOutboundEnvelope envelope)
        {
            Check.NotNull(envelope, nameof(envelope));

            var item = new OutboxStoredMessage(
                envelope.Message?.GetType(),
                await envelope.RawMessage.ReadAllAsync().ConfigureAwait(false),
                envelope.Headers,
                envelope.Endpoint.Name);

            await AddAsync(item).ConfigureAwait(false);
        }

        /// <inheritdoc cref="IOutboxReader.GetLengthAsync" />
        public Task<int> GetLengthAsync() => Task.FromResult(CommittedItemsCount);

        /// <inheritdoc cref="IOutboxReader.GetMaxAgeAsync" />
        public Task<TimeSpan> GetMaxAgeAsync() => Task.FromResult(TimeSpan.Zero);

        /// <inheritdoc cref="IOutboxReader.ReadAsync" />
        public Task<IReadOnlyCollection<OutboxStoredMessage>> ReadAsync(int count) =>
            Task.FromResult(
                (IReadOnlyCollection<OutboxStoredMessage>)Items.Take(count).Select(item => item.Item).ToList());

        /// <inheritdoc cref="IOutboxReader.RetryAsync" />
        public Task RetryAsync(OutboxStoredMessage outboxMessage)
        {
            // Nothing to do in the current implementation
            // --> the messages just stay in the queue until acknowledged
            // --> that's why reading is not thread-safe
            return Task.CompletedTask;
        }

        /// <inheritdoc cref="IOutboxReader.AcknowledgeAsync" />
        public Task AcknowledgeAsync(OutboxStoredMessage outboxMessage) =>
            RemoveAsync(outboxMessage);
    }
}
