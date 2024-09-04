// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Consuming.Transaction;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Behaviors;

/// <summary>
///     The context that is passed along the consumer behaviors pipeline.
/// </summary>
public sealed class ConsumerPipelineContext : IDisposable
{
    private readonly object _disposeLock = new();

    private IServiceScope? _serviceScope;

    private IConsumerTransactionManager? _transactionManager;

    private IRawInboundEnvelope _envelope;

    private bool _isDisposed;

    private ISilverbackContext? _silverbackContext;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ConsumerPipelineContext" /> class.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message being processed.
    /// </param>
    /// <param name="consumer">
    ///     The <see cref="IConsumer" /> that triggered this pipeline.
    /// </param>
    /// <param name="sequenceStore">
    ///     The <see cref="ISequenceStore" /> used to temporary store the pending sequences being consumed.
    /// </param>
    /// <param name="pipeline">
    ///     The behaviors composing the pipeline.
    /// </param>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" /> to be used to resolve the required services.
    /// </param>
    public ConsumerPipelineContext(
        IRawInboundEnvelope envelope,
        IConsumer consumer,
        ISequenceStore sequenceStore,
        IReadOnlyList<IConsumerBehavior> pipeline,
        IServiceProvider serviceProvider)
    {
        _envelope = Check.NotNull(envelope, nameof(envelope));
        Consumer = Check.NotNull(consumer, nameof(consumer));
        SequenceStore = Check.NotNull(sequenceStore, nameof(sequenceStore));
        Pipeline = Check.NotNull(pipeline, nameof(pipeline));
        ServiceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
    }

    /// <summary>
    ///     Gets the <see cref="IConsumer" /> that triggered this pipeline.
    /// </summary>
    public IConsumer Consumer { get; }

    /// <summary>
    ///     Gets the <see cref="ISequenceStore" /> used to temporary store the pending sequences being consumed.
    /// </summary>
    public ISequenceStore SequenceStore { get; }

    /// <summary>
    ///     Gets the behaviors composing the pipeline.
    /// </summary>
    public IReadOnlyList<IConsumerBehavior> Pipeline { get; }

    /// <summary>
    ///     Gets the <see cref="ISequence" /> that the current message belongs to.
    /// </summary>
    public ISequence? Sequence { get; private set; }

    /// <summary>
    ///     Gets a value indicating whether the current message was recognized as the beginning of a new sequence.
    /// </summary>
    public bool IsSequenceStart { get; private set; }

    /// <summary>
    ///     Gets a value indicating whether the current message was recognized as the end of the sequence.
    /// </summary>
    public bool IsSequenceEnd { get; private set; }

    /// <summary>
    ///     Gets the <see cref="IServiceProvider" /> to be used to resolve the needed services.
    /// </summary>
    public IServiceProvider ServiceProvider { get; private set; }

    /// <summary>
    ///     Gets the <see cref="IConsumerTransactionManager" /> that is handling the current pipeline transaction.
    /// </summary>
    public IConsumerTransactionManager TransactionManager
    {
        get
        {
            if (_transactionManager == null)
                throw new InvalidOperationException("The transaction manager is not initialized.");

            return _transactionManager;
        }

        internal set
        {
            if (_transactionManager != null)
                throw new InvalidOperationException("The transaction manager is already initialized.");

            _transactionManager = value;
        }
    }

    /// <summary>
    ///     Gets the <see cref="ISilverbackContext" />.
    /// </summary>
    public ISilverbackContext SilverbackContext =>
        _silverbackContext ??= ServiceProvider.GetRequiredService<ISilverbackContext>();

    /// <summary>
    ///     Gets or sets the envelopes containing the messages being processed.
    /// </summary>
    public IRawInboundEnvelope Envelope
    {
        get => _envelope;
        set => _envelope = Check.NotNull(value, nameof(value));
    }

    /// <summary>
    ///     Gets the <see cref="Task" /> representing the message processing when it is not directly awaited (e.g. when starting the
    ///     processing of a <see cref="Sequence" />. This <see cref="Task" /> will complete when all subscribers return.
    /// </summary>
    public Task? ProcessingTask { get; internal set; }

    /// <summary>
    ///     Gets the index of the current step in the pipeline.
    /// </summary>
    public int CurrentStepIndex { get; internal set; }

    /// <summary>
    ///     Gets the identifiers to be used to commit after successful processing.
    /// </summary>
    /// <returns>
    ///     The identifiers to be used to commit.
    /// </returns>
    public IReadOnlyCollection<IBrokerMessageIdentifier> GetCommitIdentifiers() =>
        Sequence?.GetCommitIdentifiers() ?? [Envelope.BrokerMessageIdentifier];

    /// <summary>
    ///     Gets the identifiers to be used to rollback in case of error.
    /// </summary>
    /// <returns>
    ///     The identifiers to be used to rollback.
    /// </returns>
    public IReadOnlyCollection<IBrokerMessageIdentifier> GetRollbackIdentifiers() =>
        Sequence?.GetRollbackIdentifiers() ?? [Envelope.BrokerMessageIdentifier];

    /// <summary>
    ///     Replaces the <see cref="IServiceProvider" /> with the one from the specified scope.
    /// </summary>
    /// <param name="newServiceScope">
    ///     The <see cref="IServiceScope" /> to be used.
    /// </param>
    public void ReplaceServiceScope(IServiceScope newServiceScope)
    {
        _serviceScope?.Dispose();

        _serviceScope = Check.NotNull(newServiceScope, nameof(newServiceScope));
        ServiceProvider = newServiceScope.ServiceProvider;
        _silverbackContext = null;
    }

    /// <summary>
    ///     Sets the current sequence.
    /// </summary>
    /// <param name="sequence">
    ///     The <see cref="ISequence" /> being processed.
    /// </param>
    /// <param name="isSequenceStart">
    ///     A value indicating whether the current message was recognized as the beginning of a new sequence.
    /// </param>
    public void SetSequence(ISequence sequence, in bool isSequenceStart)
    {
        Sequence = sequence;
        IsSequenceStart = isSequenceStart;
    }

    /// <summary>
    ///     Sets the <see cref="IsSequenceEnd" /> property to <c>true</c>, indicating that the current message was recognized as the end of
    ///     the sequence.
    /// </summary>
    public void SetIsSequenceEnd() => IsSequenceEnd = true;

    /// <summary>
    ///     Clones the current context, optionally replacing the envelope.
    /// </summary>
    /// <param name="newEnvelope">
    ///     The new envelope to be used in the cloned context.
    /// </param>
    /// <returns>
    ///     The cloned context.
    /// </returns>
    public ConsumerPipelineContext Clone(IRawInboundEnvelope? newEnvelope = null) =>
        new(newEnvelope ?? Envelope, Consumer, SequenceStore, Pipeline, ServiceProvider)
        {
            CurrentStepIndex = CurrentStepIndex
        };

    /// <inheritdoc cref="IDisposable.Dispose" />
    public void Dispose()
    {
        lock (_disposeLock)
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
        }

        _serviceScope?.Dispose();
        _serviceScope = null;

        if (ProcessingTask is not { IsCompleted: true })
            return;

        ProcessingTask.Dispose();
        ProcessingTask = null;
    }
}
