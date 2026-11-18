// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Sequences.Unbounded;

namespace Silverback.Messaging.Consuming.Transaction;

/// <inheritdoc cref="IConsumerTransactionManager" />
public sealed class ConsumerTransactionManager : IConsumerTransactionManager
{
    private readonly ConsumerPipelineContext _context;

    private readonly ISilverbackLogger<ConsumerTransactionManager> _logger;

    private readonly System.Threading.Lock _syncLock = new();

    private readonly CancellationToken _processingCancellationToken; // Canceled by the consumer stop

    private Task? _commitTask;

    private Task<bool>? _abortTask;

    private bool _isConsumerCommitted;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ConsumerTransactionManager" /> class.
    /// </summary>
    /// <param name="context">
    ///     The current <see cref="ConsumerPipelineContext" />.
    /// </param>
    /// <param name="logger">
    ///     The <see cref="ISilverbackLogger" />.
    /// </param>
    public ConsumerTransactionManager(ConsumerPipelineContext context, ISilverbackLogger<ConsumerTransactionManager> logger)
    {
        _context = context;
        _logger = logger;
    }

    internal ConsumerTransactionManager(
        ConsumerPipelineContext context,
        ISilverbackLogger<ConsumerTransactionManager> logger,
        CancellationToken processingCancellationToken)
        : this(context, logger)
    {
        _processingCancellationToken = processingCancellationToken;
    }

    /// <inheritdoc cref="IConsumerTransactionManager.Committing" />
    public AsyncEvent<ConsumerPipelineContext> Committing { get; } = new();

    /// <inheritdoc cref="IConsumerTransactionManager.Committed" />
    public AsyncEvent<ConsumerPipelineContext> Committed { get; } = new();

    /// <inheritdoc cref="IConsumerTransactionManager.Aborting" />
    public AsyncEvent<ConsumerPipelineContext> Aborting { get; } = new();

    /// <inheritdoc cref="IConsumerTransactionManager.Aborted" />
    public AsyncEvent<ConsumerPipelineContext> Aborted { get; } = new();

    /// <inheritdoc cref="IConsumerTransactionManager.Skipping" />
    public AsyncEvent<ConsumerPipelineContext> Skipping { get; } = new();

    /// <summary>
    ///     Gets a value indicating whether the transaction was rolled back without committing the consumer (e.g. to retry the message),
    ///     so that the rolled back messages and the later ones are consumed again. It's set as soon as the rollback starts.
    /// </summary>
    internal bool IsConsumerRolledBack
    {
        get
        {
            lock (_syncLock)
            {
                return field;
            }
        }

        private set
        {
            lock (_syncLock)
            {
                field = value;
            }
        }
    }

    /// <summary>
    ///     Gets a value indicating whether the consumer was stopped because of the message, by the rollback when the error policies don't
    ///     handle the error, or when the error handling fails (see <see cref="TryStopConsumerAsync" />). It's set when the stop fails as
    ///     well, since it might have failed after being accepted.
    /// </summary>
    internal bool IsConsumerStopped { get; private set; }

    /// <summary>
    ///     Gets the start generation of the consumer stopped because of the message (see <see cref="IStopOnFailureConsumer" />), or
    ///     <c>null</c> if unknown (e.g., because the stop failed).
    /// </summary>
    internal int? StoppedConsumerStartGeneration { get; private set; }

    /// <summary>
    ///     Gets a value indicating whether the transaction can't be rolled back anymore, because it was committed to the consumer (e.g., by
    ///     the commit that failed afterwards, because a <see cref="Committed" /> handler threw) or is being committed. The rollback throws
    ///     or returns <c>false</c> then (see <see cref="RollbackAsync" />).
    /// </summary>
    internal bool IsRollbackPrevented
    {
        get
        {
            lock (_syncLock)
            {
                return IsCommittedOrCommitting();
            }
        }
    }

    /// <inheritdoc cref="IConsumerTransactionManager.CommitAsync" />
    public Task CommitAsync()
    {
        TaskCompletionSource<Task> commitStarted = new();
        Task commitTask;

        lock (_syncLock)
        {
            if (_commitTask != null)
            {
                _logger.LogConsumerTransactionAlreadyCommitted(_context.Envelope);
                return _commitTask;
            }

            if (_abortTask != null)
                throw new InvalidOperationException("The transaction already aborted.");

            // The commit (as the rollback) is assigned holding the lock but started after releasing it: the handlers it invokes
            // synchronously must not run holding it, and a reentrant call (e.g. a Committing handler rolling back) must see it as started
            commitTask = _commitTask = commitStarted.Task.Unwrap();
        }

        commitStarted.SetResult(CommitCoreAsync());
        return commitTask;
    }

    /// <inheritdoc cref="IConsumerTransactionManager.RollbackAsync" />
    public Task<bool> RollbackAsync(
        Exception? exception,
        bool commitConsumer = false,
        bool throwIfAlreadyCommitted = true,
        bool stopConsuming = true)
    {
        TaskCompletionSource<Task<bool>> rollbackStarted = new();
        Task<bool> abortTask;

        lock (_syncLock)
        {
            if (_abortTask != null)
            {
                _logger.LogConsumerTransactionAlreadyAborted(_context.Envelope);
                return _abortTask;
            }

            if (IsCommittedOrCommitting())
            {
                if (throwIfAlreadyCommitted)
                    throw new InvalidOperationException("The transaction already completed.");

                return Task.FromResult(false);
            }

            abortTask = _abortTask = AcceptRollbackAsync(commitConsumer, rollbackStarted);
        }

        // Started after releasing the lock (see CommitAsync)
        rollbackStarted.SetResult(RollbackCoreAsync(exception, commitConsumer, stopConsuming));
        return abortTask;
    }

    /// <summary>
    ///     Requests the consumer to stop because of the message, as the rollback does when the error policies don't handle the error.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains a value indicating whether
    ///     the consumer was stopped because of the message.
    /// </returns>
    internal async Task<bool> TryStopConsumerAsync()
    {
        // The stop is ignored when the processing is stale (e.g. its Kafka partition was revoked in the meantime) and when the processing
        // was canceled by a stop (the consumer might have been restarted since)
        bool isStopped;

        try
        {
            if (_context.Consumer is IStopOnFailureConsumer consumer)
            {
                int? stoppedConsumerStartGeneration = await consumer
                    .TryStopOnFailureAsync(_context.Envelope.BrokerMessageIdentifier, _processingCancellationToken)
                    .ConfigureAwait(false);

                isStopped = stoppedConsumerStartGeneration != null;

                if (isStopped)
                    StoppedConsumerStartGeneration = stoppedConsumerStartGeneration;
            }
            else
            {
                // The consumers not deriving from Consumer<TIdentifier> don't report whether the stop was accepted: it's assumed when the
                // stop cancels the processing token, also when another stop does it concurrently
                if (_processingCancellationToken.IsCancellationRequested)
                    return IsConsumerStopped;

                await _context.Consumer.StopAsync(_context.Envelope.BrokerMessageIdentifier, false).ConfigureAwait(false);
                isStopped = _processingCancellationToken.IsCancellationRequested;
            }
        }
        catch (Exception)
        {
            // The stop failed, possibly after being accepted: handled like an accepted stop, so that the client is disconnected, as when
            // the exception reaches the reader
            IsConsumerStopped = true;
            throw;
        }

        if (isStopped)
            IsConsumerStopped = true;

        return IsConsumerStopped;
    }

    /// <summary>
    ///     Checks whether the exception is the cancellation requested by the consumer stop (see <see cref="StopCancellationHelper" />),
    ///     according to the cancellation token passed to the consumer pipeline that created the transaction.
    /// </summary>
    /// <param name="exception">
    ///     The exception thrown by the processing or the commit.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the operation was canceled by the consumer stop, otherwise <c>false</c>.
    /// </returns>
    internal bool IsStopCancellation(Exception exception) =>
        StopCancellationHelper.IsStopCancellation(exception, _processingCancellationToken);

    /// <summary>
    ///     Returns a value indicating whether the consumer can be committed for the message that doesn't belong to a sequence and wasn't
    ///     processed (e.g. skipped by the error policies or by the sequencer), without implicitly committing the messages of the other
    ///     sequences, which are pending or not resolved yet, or which the consumer stop aborted without rolling them back.
    /// </summary>
    /// <returns>
    ///     A value indicating whether the consumer can be committed.
    /// </returns>
    internal bool CanCommitUnprocessedMessage() =>
        !_processingCancellationToken.IsCancellationRequested &&
        _context.SequenceStore.GetPendingSequences(includeChildren: true).Count == 0 && !HasUnresolvedSequences();

    /// <summary>
    ///     Rolls back the transaction without committing the consumer and stops the consumer, when the error handling fails (e.g., an
    ///     error policy callback throws or the move policy fails to produce the message) or when the error policies don't handle the failed
    ///     commit of a message or a sequence, as the rollback does when the error policies don't handle the error. If the transaction was
    ///     already committed or aborted (e.g., committed to the consumer by the commit that failed afterwards, or aborted by the error policy
    ///     before failing), the consumer is just stopped.
    /// </summary>
    /// <remarks>
    ///     It never throws, so that the exception thrown by the failed operation is rethrown instead. The consumer is stopped even if the
    ///     rollback fails, and the rollback failure is logged.
    /// </remarks>
    /// <param name="exception">
    ///     The exception that caused the rollback.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Logged, and the exception thrown by the failed operation is rethrown instead")]
    internal async Task RollbackAndStopConsumerAfterFailureAsync(Exception exception)
    {
        TaskCompletionSource<Task<bool>> rollbackStarted = new();
        Task<bool>? rollbackTask = null;

        lock (_syncLock)
        {
            if (_abortTask == null && !IsCommittedOrCommitting())
                rollbackTask = _abortTask = AcceptRollbackAsync(false, rollbackStarted);
        }

        if (rollbackTask == null)
        {
            await TryStopConsumerAfterFailureAsync().ConfigureAwait(false);
            return;
        }

        // Started after releasing the lock (see CommitAsync)
        rollbackStarted.SetResult(RollbackCoreAsync(exception, false, true));

        try
        {
            await rollbackTask.ConfigureAwait(false);
        }
        catch (Exception rollbackException)
        {
            // The rollback stops the consumer even when it fails (see RollbackCoreAsync). Log the failure, since nothing else logs the
            // exception thrown by the Aborting or Aborted handlers (e.g., rolling back the storage transaction of the subscribers).
            _logger.LogRollbackAfterFailureFailed(_context.Envelope, rollbackException);
        }
    }

    // A commit that failed before reaching the consumer doesn't complete the transaction: it can still be rolled back. Must be called
    // holding the lock.
    private bool IsCommittedOrCommitting() => _commitTask != null && (_isConsumerCommitted || !_commitTask.IsCompleted);

    // Returns the task of the rollback, started after releasing the lock via the specified TaskCompletionSource (see CommitAsync). Must be
    // called holding the lock.
    private Task<bool> AcceptRollbackAsync(bool commitConsumer, TaskCompletionSource<Task<bool>> rollbackStarted)
    {
        // Set as soon as the rollback is accepted, before invoking the handlers, which may fail: the consumer isn't committed in any case
        if (!commitConsumer)
            IsConsumerRolledBack = true;

        return rollbackStarted.Task.Unwrap();
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Logged by the consumer")]
    private async Task TryStopConsumerAfterFailureAsync()
    {
        try
        {
            await TryStopConsumerAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
            // The exception thrown by the failed operation is rethrown instead
        }
    }

    private async Task CommitCoreAsync()
    {
        _logger.LogConsumerTransactionCommitting(_context.Envelope);

        await Committing.InvokeAsync(_context).ConfigureAwait(false);
        await _context.Consumer.CommitAsync(_context.GetCommitIdentifiers()).ConfigureAwait(false);

        lock (_syncLock)
        {
            _isConsumerCommitted = true;
        }

        await Committed.InvokeAsync(_context).ConfigureAwait(false);

        _logger.LogConsumerTransactionCommitted(_context.Envelope);
    }

    private async Task<bool> RollbackCoreAsync(Exception? exception, bool commitConsumer, bool stopConsuming)
    {
        _logger.LogConsumerTransactionAborting(_context.Envelope, exception);

        try
        {
            await Aborting.InvokeAsync(_context).ConfigureAwait(false);
        }
        catch (Exception) when (!commitConsumer && stopConsuming)
        {
            // The rollback failed: stop the consumer anyway, as when the error handling fails (see ErrorPoliciesHelper)
            await TryStopConsumerAfterFailureAsync().ConfigureAwait(false);
            throw;
        }

        if (commitConsumer)
        {
            // If the error policy requires committing the consumer, we do it only if there are no pending or unresolved sequences
            // to prevent a skipped message (not deserialized, so not added to the sequence) to commit everything (e.g. a batch
            // completed by its max wait time, still being processed or handled by its error policies, which may roll it back).
            // The unbounded sequences don't count: their messages are committed one by one by their own pipelines, like the messages
            // outside a sequence, and with MQTT the skipped message would never be acknowledged, stalling the consumer.
            // Not while the consumer is stopping either: the stop aborts the pending sequences and removes them from the store without
            // rolling them back (Kafka consumes them again from the first uncommitted offset after the restart), so the commit could
            // commit their messages. The skipped message is consumed (and skipped) again after the restart.
            if (_context.Sequence != null ? CanCommitSequence() : CanCommitUnprocessedMessage())
            {
                await Skipping.InvokeAsync(_context).ConfigureAwait(false);

                await _context.Consumer.CommitAsync(_context.GetCommitIdentifiers()).ConfigureAwait(false);
            }
        }
        else
        {
            if (stopConsuming)
                await TryStopConsumerAsync().ConfigureAwait(false);

            await _context.Consumer.RollbackAsync(_context.GetRollbackIdentifiers()).ConfigureAwait(false);
        }

        await Aborted.InvokeAsync(_context).ConfigureAwait(false);

        _logger.LogConsumerTransactionAborted(_context.Envelope);

        return true;
    }

    // The skipped (or incomplete) sequence, e.g. a batch or a chunked message, is committed right away, unless the commit is cumulative
    // (Kafka) and would implicitly commit the messages of another sequence that isn't resolved yet (e.g. a pending batch, when a chunked
    // message fails before being added to it) or that the stop aborted without rolling it back. With MQTT, the messages of the sequence
    // must be acknowledged anyway, since nothing else would acknowledge them.
    private bool CanCommitSequence() =>
        _context.Consumer is not ICumulativeCommitConsumer ||
        !_processingCancellationToken.IsCancellationRequested && !_context.SequenceStore.Any(IsOtherUnresolvedSequence);

    private bool IsOtherUnresolvedSequence(ISequence sequence) =>
        sequence != _context.Sequence && sequence.ParentSequence != _context.Sequence && sequence is not UnboundedSequence &&
        (sequence.IsPending ||
         sequence is ISequenceImplementation { ProcessingCompletedTask.IsCompleted: false } && sequence.ParentSequence == null);

    // The sequences that aren't pending anymore but whose processing isn't completed yet, e.g. a batch completed by its max wait time
    private bool HasUnresolvedSequences() =>
        _context.SequenceStore.Any(sequence =>
            sequence is ISequenceImplementation { ProcessingCompletedTask.IsCompleted: false } and not UnboundedSequence &&
            sequence.ParentSequence == null &&
            !sequence.IsPending);
}
