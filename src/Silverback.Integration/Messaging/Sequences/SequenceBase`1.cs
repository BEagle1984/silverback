// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.Util;

namespace Silverback.Messaging.Sequences;

/// <inheritdoc cref="ISequence" />
public abstract class SequenceBase<TEnvelope> : ISequenceImplementation
    where TEnvelope : IRawInboundEnvelope
{
    private readonly MessageStreamProvider<TEnvelope> _streamProvider;

    private readonly bool _enforceTimeout;

    private readonly bool _trackIdentifiers;

    private readonly TimeSpan _timeout;

    private readonly CancellationTokenSource _abortCancellationTokenSource = new();

    private readonly object _abortLockObject = new();

    private readonly ISilverbackLogger<SequenceBase<TEnvelope>> _logger;

    private readonly TaskCompletionSource<bool> _sequencerBehaviorsTaskCompletionSource = new();

    private readonly TaskCompletionSource<bool> _processingCompleteTaskCompletionSource = new();

    private readonly SemaphoreSlim _addingSemaphoreSlim = new(1, 1);

    private TaskCompletionSource<bool>? _abortingTaskCompletionSource;

    private CancellationTokenSource? _timeoutCancellationTokenSource;

    private Dictionary<string, IBrokerMessageIdentifier>? _beginningMessageIdentifiers;

    private Dictionary<string, IBrokerMessageIdentifier>? _endMessageIdentifiers;

    private ICollection<ISequence>? _sequences;

    private bool _isDisposed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SequenceBase{TEnvelope}" /> class.
    /// </summary>
    /// <param name="sequenceId">
    ///     The identifier that is used to match the consumed messages with their belonging sequence.
    /// </param>
    /// <param name="context">
    ///     The current <see cref="ConsumerPipelineContext" />, assuming that it will be the one from which the
    ///     sequence gets published to the internal bus.
    /// </param>
    /// <param name="enforceTimeout">
    ///     Specifies whether the timeout has to be enforced.
    /// </param>
    /// <param name="timeout">
    ///     The timeout to be applied. If not specified the value of <c>Endpoint.Sequence.Timeout</c> will be
    ///     used.
    /// </param>
    /// <param name="streamProvider">
    ///     The <see cref="IMessageStreamProvider" /> to be pushed. A new one will be created if not provided.
    /// </param>
    /// <param name="trackIdentifiers">
    ///     Specifies whether the message identifiers have to be collected, in order to be used for the commit
    ///     later on.
    /// </param>
    protected SequenceBase(
        string sequenceId,
        ConsumerPipelineContext context,
        bool enforceTimeout = true,
        TimeSpan? timeout = null,
        IMessageStreamProvider? streamProvider = null,
        bool trackIdentifiers = true)
    {
        SequenceId = Check.NotNull(sequenceId, nameof(sequenceId));
        Context = Check.NotNull(context, nameof(context));

        _streamProvider = streamProvider as MessageStreamProvider<TEnvelope> ??
                          new MessageStreamProvider<TEnvelope>();

        _logger = context.ServiceProvider
            .GetRequiredService<ISilverbackLogger<SequenceBase<TEnvelope>>>();

        _enforceTimeout = enforceTimeout;
        _timeout = timeout ?? Context.Envelope.Endpoint.Configuration.Sequence.Timeout;
        ResetTimeout();
        _trackIdentifiers = trackIdentifiers;
    }

    /// <inheritdoc cref="ISequence.SequenceId" />
    public string SequenceId { get; }

    /// <inheritdoc cref="ISequence.IsPending" />
    public bool IsPending => !IsComplete && !IsAborted;

    /// <inheritdoc cref="ISequence.IsAborted" />
    public bool IsAborted => AbortReason != SequenceAbortReason.None;

    /// <inheritdoc cref="ISequence.IsBeingConsumed" />
    public bool IsBeingConsumed => _streamProvider.StreamsCount > 0;

    /// <inheritdoc cref="ISequence.Sequences" />
    public IReadOnlyCollection<ISequence> Sequences => _sequences?.AsReadOnlyCollection() ?? Array.Empty<ISequence>();

    /// <inheritdoc cref="ISequence.Context" />
    public ConsumerPipelineContext Context { get; }

    /// <inheritdoc cref="ISequenceImplementation.SequencerBehaviorsTask" />
    public Task SequencerBehaviorsTask => _sequencerBehaviorsTaskCompletionSource.Task;

    /// <inheritdoc cref="ISequenceImplementation.ProcessingCompletedTask" />
    public Task ProcessingCompletedTask => _processingCompleteTaskCompletionSource.Task;

    /// <inheritdoc cref="ISequenceImplementation.ShouldCreateNewActivity" />
    public bool ShouldCreateNewActivity => true;

    /// <inheritdoc cref="ISequence.StreamProvider" />
    public IMessageStreamProvider StreamProvider => _streamProvider;

    /// <inheritdoc cref="ISequenceImplementation.Activity" />
    public Activity? Activity { get; private set; }

    /// <inheritdoc cref="ISequence.ParentSequence" />
    public ISequence? ParentSequence { get; private set; }

    /// <inheritdoc cref="ISequence.AbortException" />
    public Exception? AbortException { get; private set; }

    /// <inheritdoc cref="ISequence.Length" />
    public int Length { get; protected set; }

    /// <inheritdoc cref="ISequence.TotalLength" />
    public int? TotalLength { get; protected set; }

    /// <inheritdoc cref="ISequence.IsNew" />
    public bool IsNew { get; private set; } = true;

    /// <inheritdoc cref="ISequence.IsCompleting" />
    public bool IsCompleting { get; private set; }

    /// <inheritdoc cref="ISequence.IsComplete" />
    public bool IsComplete { get; private set; }

    /// <inheritdoc cref="ISequence.AbortReason" />
    public SequenceAbortReason AbortReason { get; private set; }

    /// <inheritdoc cref="ISequenceImplementation.SetIsNew" />
    void ISequenceImplementation.SetIsNew(bool value) => IsNew = value;

    /// <inheritdoc cref="ISequenceImplementation.SetParentSequence" />
    void ISequenceImplementation.SetParentSequence(ISequence parentSequence) =>
        ParentSequence = parentSequence;

    /// <inheritdoc cref="ISequenceImplementation.CompleteSequencerBehaviorsTask" />
    void ISequenceImplementation.CompleteSequencerBehaviorsTask() =>
        _sequencerBehaviorsTaskCompletionSource.TrySetResult(true);

    /// <inheritdoc cref="ISequenceImplementation.NotifyProcessingCompleted" />
    void ISequenceImplementation.NotifyProcessingCompleted()
    {
        _processingCompleteTaskCompletionSource.TrySetResult(true);
        _sequences?.OfType<ISequenceImplementation>().ForEach(CompleteLinkedSequence);
    }

    /// <inheritdoc cref="ISequenceImplementation.NotifyProcessingFailed" />
    void ISequenceImplementation.NotifyProcessingFailed(Exception exception)
    {
        _processingCompleteTaskCompletionSource.TrySetException(exception);

        // Don't forward the error, it's enough to handle it once
        _sequences?.OfType<ISequenceImplementation>().ForEach(CompleteLinkedSequence);
        _sequencerBehaviorsTaskCompletionSource.TrySetResult(true);
    }

    /// <inheritdoc cref="ISequenceImplementation.SetActivity" />
    void ISequenceImplementation.SetActivity(Activity activity)
    {
        Activity = activity;
    }

    /// <inheritdoc cref="ISequence.CreateStream{TMessage}" />
    public IMessageStreamEnumerable<TMessage> CreateStream<TMessage>(IReadOnlyCollection<IMessageFilter>? filters = null) =>
        StreamProvider.CreateStream<TMessage>(filters);

    /// <inheritdoc cref="ISequence.AddAsync" />
    public ValueTask<AddToSequenceResult> AddAsync(
        IRawInboundEnvelope envelope,
        ISequence? sequence,
        bool throwIfUnhandled = true)
    {
        Check.NotNull(envelope, nameof(envelope));

        if (envelope is not TEnvelope typedEnvelope)
            throw new ArgumentException($"Expected an envelope of type {typeof(TEnvelope).Name}.");

        return AddCoreAsync(typedEnvelope, sequence, throwIfUnhandled);
    }

    /// <inheritdoc cref="ISequence.AbortAsync" />
    public ValueTask AbortAsync(SequenceAbortReason reason, Exception? exception = null)
    {
        if (reason == SequenceAbortReason.None)
            throw new ArgumentOutOfRangeException(nameof(reason), reason, "Reason not specified.");

        if (reason == SequenceAbortReason.Error && exception == null)
        {
            throw new ArgumentNullException(
                nameof(exception),
                "The exception must be specified if the reason is Error.");
        }

        return AbortCoreAsync(reason, exception);
    }

    /// <inheritdoc cref="ISequence.GetBeginningBrokerMessageIdentifiers" />
    public IReadOnlyList<IBrokerMessageIdentifier> GetBeginningBrokerMessageIdentifiers()
    {
        IReadOnlyList<IBrokerMessageIdentifier> identifiers = _beginningMessageIdentifiers?.Values.AsReadOnlyList() ??
                                                              Array.Empty<IBrokerMessageIdentifier>();

        if (_sequences != null)
        {
            identifiers = identifiers
                .Union(_sequences.SelectMany(sequence => sequence.GetBeginningBrokerMessageIdentifiers()))
                .AsReadOnlyList();
        }

        return identifiers;
    }

    /// <inheritdoc cref="ISequence.GetEndBrokerMessageIdentifiers" />
    public IReadOnlyList<IBrokerMessageIdentifier> GetEndBrokerMessageIdentifiers()
    {
        IReadOnlyList<IBrokerMessageIdentifier> identifiers = _endMessageIdentifiers?.Values.AsReadOnlyList() ??
                                                              Array.Empty<IBrokerMessageIdentifier>();

        if (_sequences != null)
        {
            identifiers = identifiers
                .Union(_sequences.SelectMany(sequence => sequence.GetEndBrokerMessageIdentifiers()))
                .AsReadOnlyList();
        }

        return identifiers;
    }

    /// <inheritdoc cref="IDisposable.Dispose" />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Adds the message to the sequence.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope to be added to the sequence.
    /// </param>
    /// <param name="sequence">
    ///     The sequence to be added to the sequence.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the
    ///     message.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask{TResult}" /> representing the asynchronous operation. The task result contains a flag indicating whether
    ///     the operation was successful and the number of streams that have been actually pushed.
    /// </returns>
    protected virtual async ValueTask<AddToSequenceResult> AddCoreAsync(TEnvelope envelope, ISequence? sequence, bool throwIfUnhandled)
    {
        await _addingSemaphoreSlim.WaitAsync().ConfigureAwait(false);

        if (IsComplete || IsCompleting)
            return AddToSequenceResult.Failed;

        if (IsAborted)
            throw new InvalidOperationException("The sequence has been aborted.");

        try
        {
            ResetTimeout();

            if (sequence != null && sequence != this)
            {
                _sequences ??= new List<ISequence>();
                _sequences.Add(sequence);
                (sequence as ISequenceImplementation)?.SetParentSequence(this);
            }
            else if (_trackIdentifiers)
            {
                TrackIdentifiers(envelope);
            }

            _abortCancellationTokenSource.Token.ThrowIfCancellationRequested();

            Length++;

            if (TotalLength != null && Length == TotalLength || IsLastMessage(envelope))
            {
                TotalLength = Length;
                IsCompleting = true;

                _logger.LogLowLevelTrace(
                    "{sequenceType} '{sequenceId}' is completing (total length {sequenceLength})...",
                    () => new object[]
                    {
                        GetType().Name,
                        SequenceId,
                        TotalLength
                    });
            }

            int pushedStreamsCount = await _streamProvider.PushAsync(
                    envelope,
                    throwIfUnhandled,
                    _abortCancellationTokenSource.Token)
                .ConfigureAwait(false);

            if (IsCompleting)
                await CompleteAsync().ConfigureAwait(false);

            return AddToSequenceResult.Success(pushedStreamsCount);
        }
        catch (OperationCanceledException)
        {
            // Ignore and consider successful, it just means that the sequence was aborted.
            return AddToSequenceResult.Success(0);
        }
        catch (Exception ex)
        {
            _logger.LogLowLevelTrace(
                ex,
                "Error occurred adding message to {sequenceType} '{sequenceId}'.",
                () => new object[]
                {
                    GetType().Name,
                    SequenceId
                });

            throw;
        }
        finally
        {
            _addingSemaphoreSlim.Release();
        }
    }

    /// <summary>
    ///     Implements the logic to recognize the last message in the sequence without relying on the TotalCount
    ///     property.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope to be added to the sequence.
    /// </param>
    /// <returns>
    ///     <c>true</c> if it is the last message, otherwise <c>false</c>.
    /// </returns>
    protected virtual bool IsLastMessage(TEnvelope envelope) => false;

    /// <summary>
    ///     Marks the sequence as complete, meaning no more messages will be pushed.
    /// </summary>
    /// <param name="cancellationToken">
    ///     A <see cref="CancellationToken" /> used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    protected virtual async ValueTask CompleteAsync(CancellationToken cancellationToken = default)
    {
        if (!IsPending)
            return;

        _logger.LogLowLevelTrace(
            "Completing {sequenceType} '{sequenceId}' (length {sequenceLength})...",
            () => new object[]
            {
                GetType().Name,
                SequenceId,
                Length
            });

        IsComplete = true;
        IsCompleting = false;

        _timeoutCancellationTokenSource?.Cancel();

        await _streamProvider.CompleteAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged  resources.
    /// </summary>
    /// <param name="disposing">
    ///     A value indicating whether the method has been called by the <c>Dispose</c> method and not from the finalizer.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;

        if (!disposing)
            return;

        _logger.LogLowLevelTrace(
            "Disposing {sequenceType} '{sequenceId}'...",
            () => new object[]
            {
                GetType().Name,
                SequenceId
            });

        _abortingTaskCompletionSource?.Task.Wait();

        _streamProvider.Dispose();
        _abortCancellationTokenSource.Dispose();

        try
        {
            _timeoutCancellationTokenSource?.Cancel();
            _timeoutCancellationTokenSource?.Dispose();
        }
        catch (OperationCanceledException)
        {
            // Ignore
        }
        finally
        {
            _timeoutCancellationTokenSource = null;
        }

        _sequences?.ForEach(sequence => sequence.Dispose());

        _logger.LogLowLevelTrace(
            "Waiting adding semaphore ({sequenceType} '{sequenceId}')...",
            () => new object[]
            {
                GetType().Name,
                SequenceId
            });

        _addingSemaphoreSlim.Wait();
        _addingSemaphoreSlim.Dispose();

        Context.Dispose();

        // If necessary cancel the SequencerBehaviorsTask (if an error occurs between the two behaviors)
        if (!SequencerBehaviorsTask.IsCompleted)
            _sequencerBehaviorsTaskCompletionSource.TrySetCanceled();

        _isDisposed = true;

        _logger.LogLowLevelTrace(
            "{sequenceType} '{sequenceId}' disposed.",
            () => new object[]
            {
                GetType().Name,
                SequenceId
            });
    }

    /// <summary>
    ///     Called when the timout is elapsed. If not overridden in a derived class, the default implementation
    ///     aborts the sequence.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    protected virtual ValueTask OnTimeoutElapsedAsync() => AbortAsync(SequenceAbortReason.IncompleteSequence);

    private void TrackIdentifiers(TEnvelope envelope)
    {
        _beginningMessageIdentifiers ??= new Dictionary<string, IBrokerMessageIdentifier>();
        _endMessageIdentifiers ??= new Dictionary<string, IBrokerMessageIdentifier>();

        string groupKey = envelope.BrokerMessageIdentifier.GroupKey ?? string.Empty;

        if (!_beginningMessageIdentifiers.ContainsKey(groupKey))
            _beginningMessageIdentifiers[groupKey] = envelope.BrokerMessageIdentifier;

        _endMessageIdentifiers[groupKey] = envelope.BrokerMessageIdentifier;
    }

    private void CompleteLinkedSequence(ISequenceImplementation sequence)
    {
        sequence.NotifyProcessingCompleted();

        if (!_trackIdentifiers)
        {
            _sequences?.Remove(sequence);
            sequence.Dispose();
        }
    }

    [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
    private void ResetTimeout()
    {
        if (!_enforceTimeout)
            return;

        try
        {
            _timeoutCancellationTokenSource?.Cancel();
        }
        catch (OperationCanceledException)
        {
            // Ignore
        }

        _timeoutCancellationTokenSource = new CancellationTokenSource();
        CancellationToken cancellationToken = _timeoutCancellationTokenSource.Token;

        Task.Run(
                async () =>
                {
                    try
                    {
                        await Task.Delay(_timeout, cancellationToken).ConfigureAwait(false);

                        await OnTimeoutElapsedAsync().ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        // Ignore
                    }
                    catch (Exception ex)
                    {
                        _logger.LogSequenceTimeoutError(this, ex);
                    }
                })
            .FireAndForget();
    }

    private async ValueTask AbortCoreAsync(SequenceAbortReason reason, Exception? exception)
    {
        bool alreadyAborted;

        lock (_abortLockObject)
        {
            alreadyAborted = IsAborted;

            if (!alreadyAborted)
            {
                _abortingTaskCompletionSource = new TaskCompletionSource<bool>();

                if (reason > AbortReason)
                {
                    AbortReason = reason;
                    AbortException = exception;
                }
            }
        }

        if (alreadyAborted)
        {
            // Multiple calls to AbortAsync should await until the sequence is aborted for real,
            // otherwise the TransactionHandlerConsumerBehavior could continue before the abort
            // is done, preventing the error policies to be correctly and successfully applied.
            await _abortingTaskCompletionSource!.Task.ConfigureAwait(false);
            return;
        }

        _logger.LogLowLevelTrace(
            AbortException,
            "Aborting {sequenceType} '{sequenceId}' ({abortReason})...",
            () => new object[]
            {
                GetType().Name,
                SequenceId,
                AbortReason
            });

        try
        {
            _timeoutCancellationTokenSource?.Cancel();
        }
        catch (OperationCanceledException)
        {
            // Ignore
        }

        await Context.SequenceStore.RemoveAsync(SequenceId).ConfigureAwait(false);
        if (await RollbackTransactionAndNotifyProcessingCompletedAsync(exception).ConfigureAwait(false))
            LogAbort();

        _streamProvider.AbortIfPending();

        _abortCancellationTokenSource.Cancel();
        _abortingTaskCompletionSource?.SetResult(true);
    }

    [SuppressMessage("", "CA1031", Justification = "Exception notified")]
    private async Task<bool> RollbackTransactionAndNotifyProcessingCompletedAsync(Exception? exception)
    {
        bool done = true;

        try
        {
            switch (AbortReason)
            {
                case SequenceAbortReason.Error:
                    if (!await ErrorPoliciesHelper.ApplyErrorPoliciesAsync(Context, exception!)
                            .ConfigureAwait(false))
                    {
                        await Context.TransactionManager.RollbackAsync(exception).ConfigureAwait(false);

                        ((ISequenceImplementation)this).NotifyProcessingFailed(exception!);
                        return true;
                    }

                    break;
                case SequenceAbortReason.EnumerationAborted:
                    await Context.TransactionManager.CommitAsync().ConfigureAwait(false);
                    done = false;
                    break;
                case SequenceAbortReason.IncompleteSequence:
                    await Context.TransactionManager.RollbackAsync(exception, true).ConfigureAwait(false);
                    break;
                case SequenceAbortReason.None:
                    throw new InvalidOperationException("Reason shouldn't be None.");
                case SequenceAbortReason.ConsumerAborted:
                case SequenceAbortReason.Disposing:
                    done = await Context.TransactionManager.RollbackAsync(
                            exception,
                            throwIfAlreadyCommitted: false,
                            stopConsuming: false)
                        .ConfigureAwait(false);
                    break;
                default:
                    await Context.TransactionManager.RollbackAsync(exception).ConfigureAwait(false);
                    break;
            }

            ((ISequenceImplementation)this).NotifyProcessingCompleted();
        }
        catch (Exception newException)
        {
            ((ISequenceImplementation)this).NotifyProcessingFailed(newException);
        }

        return done;
    }

    private void LogAbort()
    {
        switch (AbortReason)
        {
            case SequenceAbortReason.Error:
                _logger.LogSequenceProcessingError(this, AbortException!);
                break;
            case SequenceAbortReason.IncompleteSequence:
                _logger.LogIncompleteSequenceAborted(this);
                break;
            default:
                _logger.LogSequenceAborted(this, AbortReason);
                break;
        }
    }
}
