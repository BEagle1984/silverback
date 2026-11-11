// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
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

    private volatile bool _isConsumerRolledBack;

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

    /// <summary>
    ///     Gets a value indicating whether the transaction was rolled back without committing the consumer (e.g. to retry the message),
    ///     so that the rolled back messages and the later ones are consumed again. It's set as soon as the rollback starts.
    /// </summary>
    internal bool IsConsumerRolledBack => _isConsumerRolledBack;

    /// <inheritdoc cref="IConsumerTransactionManager.CommitAsync" />
    public Task CommitAsync()
    {
        lock (_syncLock)
        {
            if (_commitTask != null)
            {
                _logger.LogConsumerTransactionAlreadyCommitted(_context.Envelope);
                return _commitTask;
            }

            if (_abortTask != null)
                throw new InvalidOperationException("The transaction already aborted.");

            return _commitTask = CommitCoreAsync();
        }
    }

    /// <inheritdoc cref="IConsumerTransactionManager.RollbackAsync" />
    public Task<bool> RollbackAsync(
        Exception? exception,
        bool commitConsumer = false,
        bool throwIfAlreadyCommitted = true,
        bool stopConsuming = true)
    {
        lock (_syncLock)
        {
            if (_abortTask != null)
            {
                _logger.LogConsumerTransactionAlreadyAborted(_context.Envelope);
                return _abortTask;
            }

            // A commit that failed before reaching the consumer doesn't complete the transaction: it can still be rolled back
            if (_commitTask != null && (_isConsumerCommitted || !_commitTask.IsCompleted))
            {
                if (throwIfAlreadyCommitted)
                    throw new InvalidOperationException("The transaction already completed.");

                return Task.FromResult(false);
            }

            return _abortTask = RollbackCoreAsync(exception, commitConsumer, stopConsuming);
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

        // Set before invoking the handlers, which may fail: the consumer isn't committed in any case
        if (!commitConsumer)
            _isConsumerRolledBack = true;

        await Aborting.InvokeAsync(_context).ConfigureAwait(false);

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
            if (_context.Sequence != null
                    ? CanCommitSequence()
                    : !_processingCancellationToken.IsCancellationRequested &&
                      _context.SequenceStore.GetPendingSequences(includeChildren: true).Count == 0 && !HasUnresolvedSequences())
            {
                await _context.Consumer.CommitAsync(_context.GetCommitIdentifiers()).ConfigureAwait(false);
            }
        }
        else
        {
            if (stopConsuming)
                await _context.Consumer.StopAsync(_context.Envelope.BrokerMessageIdentifier, false).ConfigureAwait(false);

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
