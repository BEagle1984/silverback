// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;

namespace Silverback.Messaging.Consuming.Transaction;

/// <inheritdoc cref="IConsumerTransactionManager" />
public sealed class ConsumerTransactionManager : IConsumerTransactionManager
{
    private readonly ConsumerPipelineContext _context;

    private readonly ISilverbackLogger<ConsumerTransactionManager> _logger;

    private readonly System.Threading.Lock _syncLock = new();

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

    /// <inheritdoc cref="IConsumerTransactionManager.Committing" />
    public AsyncEvent<ConsumerPipelineContext> Committing { get; } = new();

    /// <inheritdoc cref="IConsumerTransactionManager.Committed" />
    public AsyncEvent<ConsumerPipelineContext> Committed { get; } = new();

    /// <inheritdoc cref="IConsumerTransactionManager.Aborting" />
    public AsyncEvent<ConsumerPipelineContext> Aborting { get; } = new();

    /// <inheritdoc cref="IConsumerTransactionManager.Aborted" />
    public AsyncEvent<ConsumerPipelineContext> Aborted { get; } = new();

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

        await Aborting.InvokeAsync(_context).ConfigureAwait(false);

        if (commitConsumer)
        {
            // If the error policy requires committing the consumer, we do it only if there are no pending sequences
            // to prevent a skipped message (not deserialized, so not added to the sequence) to commit everything.
            // The unbounded sequences don't count: their messages are committed one by one by their own pipelines, like the messages
            // outside a sequence, and with MQTT the skipped message would never be acknowledged, stalling the consumer.
            if (_context.Sequence != null || _context.SequenceStore.GetPendingSequences(includeChildren: true).Count == 0)
                await _context.Consumer.CommitAsync(_context.GetCommitIdentifiers()).ConfigureAwait(false);
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
}
