// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Connectors.Repositories.Model;
using Silverback.Messaging.Outbound.TransactionalOutbox;

namespace Silverback.Messaging.Connectors.Repositories
{
    /// <summary>
    ///     Exposes the methods to read from the outbox. Used by the <see cref="IOutboxWorker" />.
    /// </summary>
    public interface IOutboxReader
    {
        /// <summary>
        ///     Returns the total number of messages in the outbox.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains the
        ///     number of messages in the outbox.
        /// </returns>
        Task<int> GetLengthAsync();

        /// <summary>
        ///     Gets a <see cref="TimeSpan" /> representing the time elapsed since the oldest message currently in
        ///     the outbox was written.
        /// </summary>
        /// <returns>
        ///     The age of the oldest message.
        /// </returns>
        Task<TimeSpan> GetMaxAgeAsync();

        /// <summary>
        ///     Reads the specified number of messages from the outbox (according to the FIFO rule). The operation must
        ///     be acknowledged for the messages to be removed.
        /// </summary>
        /// <param name="count">
        ///     The number of items to be dequeued.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the result of the asynchronous operation. The task result
        ///     contains the collection of <see cref="OutboxStoredMessage" />.
        /// </returns>
        Task<IReadOnlyCollection<OutboxStoredMessage>> ReadAsync(int count);

        /// <summary>
        ///     Called after the message has been successfully produced to remove it from the outbox.
        /// </summary>
        /// <param name="outboxMessage">
        ///     The message that was processed.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the result of the asynchronous operation.
        /// </returns>
        Task AcknowledgeAsync(OutboxStoredMessage outboxMessage);

        /// <summary>
        ///     Called when an error occurs producing the message to re-enqueue it and retry later on.
        /// </summary>
        /// <param name="outboxMessage">
        ///     The message that was processed.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the result of the asynchronous operation.
        /// </returns>
        Task RetryAsync(OutboxStoredMessage outboxMessage);
    }
}
