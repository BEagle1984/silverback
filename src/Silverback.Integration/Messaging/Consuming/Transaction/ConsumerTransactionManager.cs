// Copyright (c) 2023 Sergio Aquilini
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

    private readonly IConsumerLogger<ConsumerTransactionManager> _logger;

    private bool _isAborted;

    private bool _isCommitted;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ConsumerTransactionManager" /> class.
    /// </summary>
    /// <param name="context">
    ///     The current <see cref="ConsumerPipelineContext" />.
    /// </param>
    /// <param name="logger">
    ///     The <see cref="ISilverbackLogger" />.
    /// </param>
    public ConsumerTransactionManager(
        ConsumerPipelineContext context,
        IConsumerLogger<ConsumerTransactionManager> logger)
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
    public async Task CommitAsync()
    {
        if (_isCommitted)
        {
            _logger.LogConsumerLowLevelTrace("Not committing consumer transaction because it was already committed.", _context.Envelope);

            return;
        }

        if (_isAborted)
            throw new InvalidOperationException("The transaction already aborted.");

        _isCommitted = true;

        _logger.LogConsumerLowLevelTrace("Committing consumer transaction...", _context.Envelope);

        await Committing.InvokeAsync(_context).ConfigureAwait(false);
        await _context.Consumer.CommitAsync(_context.GetCommitIdentifiers()).ConfigureAwait(false);
        await Committed.InvokeAsync(_context).ConfigureAwait(false);

        _logger.LogConsumerLowLevelTrace("Consumer transaction committed.", _context.Envelope);
    }

    /// <inheritdoc cref="IConsumerTransactionManager.RollbackAsync" />
    public async Task<bool> RollbackAsync(
        Exception? exception,
        bool commitConsumer = false,
        bool throwIfAlreadyCommitted = true,
        bool stopConsuming = true)
    {
        if (_isAborted)
        {
            _logger.LogConsumerLowLevelTrace("Not aborting consumer transaction because it was already aborted.", _context.Envelope);

            return false;
        }

        if (_isCommitted)
        {
            if (throwIfAlreadyCommitted)
                throw new InvalidOperationException("The transaction already completed.");

            return false;
        }

        _isAborted = true;

        _logger.LogConsumerLowLevelTrace("Aborting consumer transaction...", _context.Envelope, exception);

        await Aborting.InvokeAsync(_context).ConfigureAwait(false);

        if (commitConsumer)
        {
            await _context.Consumer.CommitAsync(_context.GetCommitIdentifiers()).ConfigureAwait(false);
        }
        else
        {
            if (stopConsuming)
                await _context.Consumer.StopAsync(false).ConfigureAwait(false);

            await _context.Consumer.RollbackAsync(_context.GetRollbackIdentifiers()).ConfigureAwait(false);
        }

        await Aborted.InvokeAsync(_context).ConfigureAwait(false);

        _logger.LogConsumerLowLevelTrace("Consumer transaction aborted.", _context.Envelope);

        return true;
    }
}
