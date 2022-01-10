// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Silverback.Messaging.Outbound.TransactionalOutbox;

/// <summary>
///     Exposes the methods to read from the outbox.
/// </summary>
/// <remarks>
///     Used by the <see cref="IOutboxWorker" />.
/// </remarks>
public interface IOutboxReader
{
    /// <summary>
    ///     Reads the specified number of messages from the outbox (according to the FIFO rule). The operation must be acknowledged for the
    ///     messages to be removed.
    /// </summary>
    /// <param name="count">
    ///     The number of items to be dequeued.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the result of the asynchronous operation. The task result
    ///     contains the collection of <see cref="OutboxMessage" />.
    /// </returns>
    Task<IReadOnlyCollection<OutboxMessage>> GetAsync(int count);

    /// <summary>
    ///     Returns the total number of messages in the outbox.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains the number of messages in the
    ///     outbox.
    /// </returns>
    Task<int> GetLengthAsync();

    /// <summary>
    ///     Gets a <see cref="TimeSpan" /> representing the time elapsed since the oldest message currently in the outbox was written.
    /// </summary>
    /// <returns>
    ///     The age of the oldest message.
    /// </returns>
    Task<TimeSpan> GetMaxAgeAsync();

    /// <summary>
    ///     Called after the messages have been successfully produced to remove them from the outbox.
    /// </summary>
    /// <param name="outboxMessages">
    ///     The acknowledged messages.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the result of the asynchronous operation.
    /// </returns>
    Task AcknowledgeAsync(IEnumerable<OutboxMessage> outboxMessages);
}
