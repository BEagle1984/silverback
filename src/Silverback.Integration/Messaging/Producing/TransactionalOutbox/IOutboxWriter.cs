// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Messaging.Producing.TransactionalOutbox;

/// <summary>
///     Exposes the methods to write to the outbox.
/// </summary>
/// <remarks>
///     Used by the <see cref="OutboxProduceStrategy" />.
/// </remarks>
public interface IOutboxWriter
{
    /// <summary>
    ///     Adds the message contained in the specified envelope to the outbox.
    /// </summary>
    /// <param name="outboxMessage">
    ///     The message to be stored in the outbox.
    /// </param>
    /// <param name="context">
    ///     The <see cref="ISilverbackContext" /> in the current scope.
    /// </param>
    /// <param name="cancellationToken">
    ///     The cancellation token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task AddAsync(
        OutboxMessage outboxMessage,
        ISilverbackContext? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Adds the message contained in the specified envelope to the outbox.
    /// </summary>
    /// <param name="outboxMessages">
    ///     The messages to be stored in the outbox.
    /// </param>
    /// <param name="context">
    ///     The <see cref="ISilverbackContext" /> in the current scope.
    /// </param>
    /// <param name="cancellationToken">
    ///     The cancellation token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task AddAsync(
        IEnumerable<OutboxMessage> outboxMessages,
        ISilverbackContext? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Adds the message contained in the specified envelope to the outbox.
    /// </summary>
    /// <param name="outboxMessages">
    ///     The messages to be stored in the outbox.
    /// </param>
    /// <param name="context">
    ///     The <see cref="ISilverbackContext" /> in the current scope.
    /// </param>
    /// <param name="cancellationToken">
    ///     The cancellation token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    /// <remarks>
    ///     The <see cref="OutboxProduceStrategy" /> writes the batches through this overload, while the messages are still being
    ///     produced. The implementation should await with <c>ConfigureAwait(false)</c>, otherwise the synchronous publishing methods
    ///     could deadlock when called on a thread with a synchronization context (e.g. a UI thread).
    /// </remarks>
    Task AddAsync(
        IAsyncEnumerable<OutboxMessage> outboxMessages,
        ISilverbackContext? context = null,
        CancellationToken cancellationToken = default);
}
