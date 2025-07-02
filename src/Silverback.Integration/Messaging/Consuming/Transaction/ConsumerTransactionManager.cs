// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;

namespace Silverback.Messaging.Consuming.Transaction;

/// <inheritdoc cref="IConsumerTransactionManager" />
public sealed class ConsumerTransactionManager : IConsumerTransactionManager
{
    private readonly ConsumerPipelineContext _context;

    private readonly ISilverbackLogger<ConsumerTransactionManager> _logger;

    private readonly object _syncLock = new();

    private Task? _commitTask;

    private Task<bool>? _abortTask;

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
                _logger.LogProcessingTrace(_context.Envelope, "Not committing consumer transaction because it was already committed.");
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
                _logger.LogProcessingTrace(_context.Envelope, "Not aborting consumer transaction because it was already aborted.");
                return _abortTask;
            }

            if (_commitTask != null)
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
        _logger.LogProcessingTrace(_context.Envelope, "Committing consumer transaction...");

        await Committing.InvokeAsync(_context).ConfigureAwait(false);
        await _context.Consumer.CommitAsync(_context.GetCommitIdentifiers()).ConfigureAwait(false);
        await Committed.InvokeAsync(_context).ConfigureAwait(false);

        _logger.LogProcessingTrace(_context.Envelope, "Consumer transaction committed.");
    }

    private async Task<bool> RollbackCoreAsync(Exception? exception, bool commitConsumer, bool stopConsuming)
    {
        _logger.LogProcessingTrace(_context.Envelope, exception, "Aborting consumer transaction...");

        await Aborting.InvokeAsync(_context).ConfigureAwait(false);

        if (commitConsumer)
        {
            // If the error policy requires committing the consumer, we do it only if there are no pending sequences
            // to prevent a skipped message (not deserialized, so not added to the sequence) to commit everything.
            if (_context.Sequence != null || !_context.SequenceStore.Any(sequence => sequence.IsPending))
                await _context.Consumer.CommitAsync(_context.GetCommitIdentifiers()).ConfigureAwait(false);
        }
        else
        {
            if (stopConsuming)
                await _context.Consumer.StopAsync(false).ConfigureAwait(false);

            await _context.Consumer.RollbackAsync(_context.GetRollbackIdentifiers()).ConfigureAwait(false);
        }

        await Aborted.InvokeAsync(_context).ConfigureAwait(false);

        _logger.LogProcessingTrace(_context.Envelope, "Consumer transaction aborted.");

        return true;
    }
}
