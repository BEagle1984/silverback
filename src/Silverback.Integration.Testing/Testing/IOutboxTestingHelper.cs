// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Producing.TransactionalOutbox;

namespace Silverback.Testing;

/// <summary>
///     Exposes the helper methods that simplify testing the applications using the transactional outbox.
/// </summary>
public interface IOutboxTestingHelper
{
    /// <summary>
    ///     Returns the messages that are currently pending in the outbox, without removing them.
    /// </summary>
    /// <param name="outboxSettings">
    ///     The settings of the outbox to read. If not specified, all outboxes are considered.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask{TResult}" /> representing the asynchronous operation. The task result contains the pending messages.
    /// </returns>
    ValueTask<IReadOnlyList<OutboxMessage>> GetMessagesAsync(OutboxSettings? outboxSettings = null);

    /// <summary>
    ///     Returns the number of messages that are currently pending in the outbox.
    /// </summary>
    /// <param name="outboxSettings">
    ///     The settings of the outbox to read. If not specified, all outboxes are considered.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask{TResult}" /> representing the asynchronous operation. The task result contains the number of pending
    ///     messages.
    /// </returns>
    ValueTask<int> GetLengthAsync(OutboxSettings? outboxSettings = null);

    /// <summary>
    ///     Checks whether the outbox is empty.
    /// </summary>
    /// <param name="outboxSettings">
    ///     The settings of the outbox to check. If not specified, all outboxes are considered.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask{TResult}" /> representing the asynchronous operation. The task result contains <c>true</c> if the outbox
    ///     is empty, otherwise <c>false</c>.
    /// </returns>
    ValueTask<bool> IsEmptyAsync(OutboxSettings? outboxSettings = null);

    /// <summary>
    ///     Returns a <see cref="ValueTask" /> that completes when the outboxes being drained have been emptied.
    /// </summary>
    /// <param name="timeout">
    ///     The time to wait for the outbox to be empty. The default is 30 seconds.
    /// </param>
    /// <param name="outboxSettings">
    ///     The settings of the outbox to wait for. If not specified, only the outboxes being drained by a registered and not paused
    ///     worker are considered.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> that completes when the outbox is empty.
    /// </returns>
    /// <exception cref="TimeoutException">
    ///     The timeout elapsed before the outbox was emptied.
    /// </exception>
    ValueTask WaitUntilEmptyAsync(TimeSpan? timeout = null, OutboxSettings? outboxSettings = null);

    /// <summary>
    ///     Returns a <see cref="ValueTask" /> that completes when the outboxes being drained have been emptied.
    /// </summary>
    /// <param name="cancellationToken">
    ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
    /// </param>
    /// <param name="outboxSettings">
    ///     The settings of the outbox to wait for. If not specified, only the outboxes being drained by a registered and not paused
    ///     worker are considered.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> that completes when the outbox is empty.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    ///     The <paramref name="cancellationToken" /> was canceled before the outbox was emptied.
    /// </exception>
    ValueTask WaitUntilEmptyAsync(CancellationToken cancellationToken, OutboxSettings? outboxSettings = null);

    /// <summary>
    ///     Removes all the pending messages from the outbox, without producing them.
    /// </summary>
    /// <param name="outboxSettings">
    ///     The settings of the outbox to clear. If not specified, all outboxes are cleared.
    /// </param>
    /// <param name="cancellationToken">
    ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation.
    /// </returns>
    ValueTask ClearAsync(OutboxSettings? outboxSettings = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Runs the registered outbox workers right away, until the outboxes are empty. This is especially useful in combination with the
    ///     <c>UseManualOutboxWorker</c> configuration.
    /// </summary>
    /// <remarks>
    ///     The distributed lock is bypassed and the exceptions are propagated to the caller.
    /// </remarks>
    /// <param name="cancellationToken">
    ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///     No outbox worker is registered.
    /// </exception>
    ValueTask ProcessAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Runs the outbox worker processing the specified outbox right away, until the outbox is empty.
    /// </summary>
    /// <remarks>
    ///     The distributed lock is bypassed and the exceptions are propagated to the caller.
    /// </remarks>
    /// <param name="outboxSettings">
    ///     The settings of the outbox to process.
    /// </param>
    /// <param name="cancellationToken">
    ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///     No outbox worker is registered for the specified outbox.
    /// </exception>
    ValueTask ProcessAsync(OutboxSettings outboxSettings, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Stops the registered outbox workers from processing the outboxes at regular intervals. The returned
    ///     <see cref="ValueTask" /> completes when no worker is processing the outbox anymore.
    /// </summary>
    /// <param name="cancellationToken">
    ///     A <see cref="CancellationToken" /> to observe while waiting for the running batch to complete. The cancellation only stops
    ///     the wait: the workers paused so far stay paused until <c>ResumeWorker</c> is called.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///     No outbox worker is registered.
    /// </exception>
    ValueTask PauseWorkerAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Stops the outbox worker processing the specified outbox from processing it at regular intervals. The returned
    ///     <see cref="ValueTask" /> completes when the worker is not processing the outbox anymore.
    /// </summary>
    /// <param name="outboxSettings">
    ///     The settings of the outbox.
    /// </param>
    /// <param name="cancellationToken">
    ///     A <see cref="CancellationToken" /> to observe while waiting for the running batch to complete. The cancellation only stops
    ///     the wait: the worker stays paused until <c>ResumeWorker</c> is called.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///     No outbox worker is registered for the specified outbox.
    /// </exception>
    ValueTask PauseWorkerAsync(OutboxSettings outboxSettings, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Resumes the processing of the outboxes at regular intervals, after <c>PauseWorkerAsync</c>. It resumes the workers suspended
    ///     by <c>UseManualOutboxWorker</c> as well.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///     No outbox worker is registered.
    /// </exception>
    void ResumeWorker();

    /// <summary>
    ///     Resumes the processing of the specified outbox at regular intervals, after <c>PauseWorkerAsync</c>. It resumes the worker
    ///     suspended by <c>UseManualOutboxWorker</c> as well.
    /// </summary>
    /// <param name="outboxSettings">
    ///     The settings of the outbox.
    /// </param>
    /// <exception cref="InvalidOperationException">
    ///     No outbox worker is registered for the specified outbox.
    /// </exception>
    void ResumeWorker(OutboxSettings outboxSettings);
}
