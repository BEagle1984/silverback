// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.BrokerMessageIdentifiersTracking;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.Util;
using ActivitySources = Silverback.Messaging.Diagnostics.ActivitySources;

namespace Silverback.Messaging.Sequences;

/// <inheritdoc cref="ISequence" />
public abstract class SequenceBase<TEnvelope> : ISequenceImplementation
    where TEnvelope : IRawInboundEnvelope
{
    private readonly MessageStreamProvider<TEnvelope> _streamProvider;

    private readonly bool _enforceTimeout;

    private readonly IBrokerMessageIdentifiersTracker? _identifiersTracker;

    private readonly TimeSpan _timeout;

    private readonly CancellationTokenSource _addCancellationTokenSource = new();

    private readonly ISilverbackLogger<SequenceBase<TEnvelope>> _logger;

    private readonly TaskCompletionSource<bool> _sequencerBehaviorsTaskCompletionSource = new();

    private readonly TaskCompletionSource<bool> _processingCompleteTaskCompletionSource = new();

    private readonly SemaphoreSlim _addSemaphoreSlim = new(1, 1);

    private readonly SemaphoreSlim _completeSemaphoreSlim = new(1, 1);

    private CompleteState _completeState = CompleteState.NotComplete;

    private TaskCompletionSource<bool>? _abortingTaskCompletionSource;

    private DateTime _timeoutExpiration = DateTime.MaxValue;

    private List<ISequence>? _sequences;

    private bool _isDisposed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SequenceBase{TEnvelope}" /> class.
    /// </summary>
    /// <param name="sequenceId">
    ///     The identifier that is used to match the consumed messages with their belonging sequence.
    /// </param>
    /// <param name="context">
    ///     The current <see cref="ConsumerPipelineContext" />, assuming that it will be the one from which the
    ///     sequence gets published via the message bus.
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
    ///     Specifies whether the message identifiers have to be collected to be used for the commit
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

        _logger = context.ServiceProvider.GetRequiredService<ISilverbackLogger<SequenceBase<TEnvelope>>>();

        _enforceTimeout = enforceTimeout;

        _timeout = timeout ?? Context.Envelope.Endpoint.Configuration.Sequence.Timeout;
        if (_enforceTimeout)
            InitTimeoutTimer();

        _identifiersTracker = trackIdentifiers
            ? context.ServiceProvider.GetRequiredService<IBrokerMessageIdentifiersTrackerFactory>()
                .GetTracker(context.Envelope.Endpoint.Configuration, context.ServiceProvider)
            : null;
    }

    private enum CompleteState
    {
        NotComplete,

        Complete,

        Aborted
    }

    /// <inheritdoc cref="ISequence.SequenceId" />
    public string SequenceId { get; }

    /// <inheritdoc cref="ISequence.IsPending" />
    public bool IsPending => _completeState == CompleteState.NotComplete;

    /// <inheritdoc cref="ISequence.IsAborted" />
    public bool IsAborted => _completeState == CompleteState.Aborted;

    /// <inheritdoc cref="ISequence.IsBeingConsumed" />
    public bool IsBeingConsumed => _streamProvider.StreamsCount > 0;

    /// <inheritdoc cref="ISequence.Sequences" />
    public IReadOnlyCollection<ISequence> Sequences => _sequences?.AsReadOnlyCollection() ?? [];

    /// <inheritdoc cref="ISequence.Context" />
    public ConsumerPipelineContext Context { get; }

    /// <inheritdoc cref="ISequenceImplementation.SequencerBehaviorsTask" />
    public Task SequencerBehaviorsTask => _sequencerBehaviorsTaskCompletionSource.Task;

    /// <inheritdoc cref="ISequenceImplementation.ProcessingCompletedTask" />
    public Task ProcessingCompletedTask => _processingCompleteTaskCompletionSource.Task;

    /// <inheritdoc cref="ISequence.StreamProvider" />
    public IMessageStreamProvider StreamProvider => _streamProvider;

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
    public bool IsComplete => _completeState == CompleteState.Complete;

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
        _sequences?.OfType<ISequenceImplementation>().ToList().ForEach(CompleteLinkedSequence); // Copy the list to avoid concurrent modification exception
    }

    /// <inheritdoc cref="ISequenceImplementation.NotifyProcessingFailed" />
    void ISequenceImplementation.NotifyProcessingFailed(Exception exception)
    {
        _processingCompleteTaskCompletionSource.TrySetException(exception);

        // Don't forward the error, it's enough to handle it once
        _sequences?.OfType<ISequenceImplementation>().ToList().ForEach(CompleteLinkedSequence); // Copy the list to avoid concurrent modification exception
        _sequencerBehaviorsTaskCompletionSource.TrySetResult(true);
    }

    /// <inheritdoc cref="ISequence.CreateStream{TMessage}" />
    public IMessageStreamEnumerable<TMessage> CreateStream<TMessage>(IReadOnlyCollection<IMessageFilter>? filters = null) =>
        StreamProvider.CreateStream<TMessage>(filters);

    /// <inheritdoc cref="ISequence.AddAsync" />
    public async Task<AddToSequenceResult> AddAsync(IRawInboundEnvelope envelope, ISequence? sequence, bool throwIfUnhandled)
    {
        Check.NotNull(envelope, nameof(envelope));

        if (envelope is not TEnvelope typedEnvelope)
            throw new ArgumentException($"Expected an envelope of type {typeof(TEnvelope).Name}.");

        if (!await WaitIfNotDisposedAsync(_addSemaphoreSlim).ConfigureAwait(false))
            return AddToSequenceResult.AbortedOrFailed(IsAborted, _abortingTaskCompletionSource?.Task);

        bool lockReleased = false;

        try
        {
            return await AddCoreAsync(typedEnvelope, sequence, throwIfUnhandled).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogTrace(
                ex,
                "Operation canceled, skipping add to {sequenceType} '{sequenceId}' and waiting until it is completed.",
                () => [GetType().Name, SequenceId]);

            _addSemaphoreSlim.Release(); // Release the semaphore to allow the abort to complete and avoid deadlocks
            lockReleased = true;

            await _processingCompleteTaskCompletionSource.Task.ConfigureAwait(false);

            return AddToSequenceResult.AbortedOrFailed(IsAborted, _abortingTaskCompletionSource?.Task);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(
                ex,
                "Error occurred adding message to {sequenceType} '{sequenceId}'.",
                () => [GetType().Name, SequenceId]);

            throw;
        }
        finally
        {
            if (!lockReleased)
                _addSemaphoreSlim.Release();
        }
    }

    /// <inheritdoc cref="ISequence.AbortIfIncompleteAsync" />
    public async Task AbortIfIncompleteAsync()
    {
        if (!await WaitIfNotDisposedAsync(_completeSemaphoreSlim).ConfigureAwait(false))
            return;
        try
        {
            if (!IsPending)
                return;

            await AbortCoreAsync(SequenceAbortReason.IncompleteSequence, null).ConfigureAwait(false);
        }
        finally
        {
            _completeSemaphoreSlim.Release();
        }
    }

    /// <inheritdoc cref="ISequence.AbortAsync" />
    public async Task AbortAsync(SequenceAbortReason reason, Exception? exception = null)
    {
        if (reason == SequenceAbortReason.None)
            throw new ArgumentOutOfRangeException(nameof(reason), reason, "Reason not specified.");

        if (reason == SequenceAbortReason.Error && exception == null)
            throw new ArgumentNullException(nameof(exception), "The exception must be specified if the reason is Error.");

        if (!await WaitIfNotDisposedAsync(_completeSemaphoreSlim).ConfigureAwait(false))
            return;

        try
        {
            await AbortCoreAsync(reason, exception).ConfigureAwait(false);
        }
        finally
        {
            _completeSemaphoreSlim.Release();
        }
    }

    /// <inheritdoc cref="ISequence.GetCommitIdentifiers" />
    public IReadOnlyCollection<IBrokerMessageIdentifier> GetCommitIdentifiers()
    {
        // Merge identifiers from the child sequences to ensure that they are all taken into account for the commit
        if (_sequences != null && _identifiersTracker != null)
        {
            foreach (IBrokerMessageIdentifier offset in _sequences.SelectMany(sequence => sequence.GetCommitIdentifiers()))
            {
                _identifiersTracker?.TrackIdentifier(offset);
            }
        }

        return _identifiersTracker?.GetCommitIdentifiers() ?? [];
    }

    /// <inheritdoc cref="ISequence.GetRollbackIdentifiers" />
    public IReadOnlyCollection<IBrokerMessageIdentifier> GetRollbackIdentifiers()
    {
        // Merge identifiers from the child sequences to ensure that they are all taken into account for the rollback
        if (_sequences != null && _identifiersTracker != null)
        {
            foreach (IBrokerMessageIdentifier offset in _sequences.SelectMany(sequence => sequence.GetRollbackIdentifiers()))
            {
                _identifiersTracker?.TrackIdentifier(offset);
            }
        }

        return _identifiersTracker?.GetRollbackIdentifiers() ?? [];
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
    ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains a flag indicating whether
    ///     the operation was successful and the number of streams that have been actually pushed.
    /// </returns>
    protected virtual async Task<AddToSequenceResult> AddCoreAsync(TEnvelope envelope, ISequence? sequence, bool throwIfUnhandled)
    {
        if (!IsPending)
            return AddToSequenceResult.AbortedOrFailed(IsAborted, _abortingTaskCompletionSource?.Task);

        if (_enforceTimeout)
        {
            if (IsTimeoutElapsed())
                throw new OperationCanceledException("The sequence timed out.");

            ResetTimeout();
        }

        if (sequence != null && sequence != this)
        {
            _sequences ??= [];
            _sequences.Add(sequence);
            (sequence as ISequenceImplementation)?.SetParentSequence(this);
        }

        int pushedStreamsCount;
        try
        {
            pushedStreamsCount = await _streamProvider.PushAsync(
                    envelope,
                    throwIfUnhandled,
                    static envelope => ActivitySources.UpdateConsumeActivity((IRawInboundEnvelope)envelope!),
                    envelope)
                .ConfigureAwait(false);

            // If no stream was pushed, the message was ignored (throwIfUnhandled must be false)
            if (pushedStreamsCount == 0)
                return AddToSequenceResult.Success(0);
        }
        catch (OperationCanceledException)
        {
            // Consider the message as successfully processed if the enumeration was aborted and track the related identifier
            if (IsAborted && AbortReason == SequenceAbortReason.EnumerationAborted)
                _identifiersTracker?.TrackIdentifier(envelope.BrokerMessageIdentifier);

            throw;
        }

        Length++; // Increment only after checking that the message was pushed to at least one stream

        // Track identifier only after successfully processing to ensure that no message is lost (committing message that was excluded from the batch)
        _identifiersTracker?.TrackIdentifier(envelope.BrokerMessageIdentifier);

        if (Length == TotalLength || IsLastMessage(envelope))
        {
            TotalLength = Length;

            _logger.LogTrace(
                "{sequenceType} '{sequenceId}' is completing (total length {sequenceLength})...",
                () => [GetType().Name, SequenceId, TotalLength]);

            await _completeSemaphoreSlim.WaitAsync().ConfigureAwait(false);

            try
            {
                await CompleteCoreAsync().ConfigureAwait(false);
            }
            finally
            {
                _completeSemaphoreSlim.Release();
            }
        }

        return AddToSequenceResult.Success(pushedStreamsCount);
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
    protected virtual async Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        if (!await WaitIfNotDisposedAsync(_completeSemaphoreSlim).ConfigureAwait(false))
            return;

        try
        {
            await CompleteCoreAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _completeSemaphoreSlim.Release();
        }
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

        _logger.LogTrace("Disposing {sequenceType} '{sequenceId}'...", () => [GetType().Name, SequenceId]);

        _abortingTaskCompletionSource?.Task.SafeWait();

        _addCancellationTokenSource.Cancel();
        _streamProvider.Dispose();

        _sequences?.ForEach(sequence => sequence.Dispose());

        _logger.LogTrace("Dispose is waiting add semaphore ({sequenceType} '{sequenceId}')...", () => [GetType().Name, SequenceId]);
        _addSemaphoreSlim.Wait();
        _logger.LogTrace("Dispose is waiting complete semaphore ({sequenceType} '{sequenceId}')...", () => [GetType().Name, SequenceId]);
        _completeSemaphoreSlim.Wait();
        _isDisposed = true;

        _addSemaphoreSlim.Dispose();
        _addCancellationTokenSource.Dispose();
        _completeSemaphoreSlim.Dispose();

        // If necessary, cancel the SequencerBehaviorsTask (if an error occurs between the two behaviors)
        if (!SequencerBehaviorsTask.IsCompleted)
            _sequencerBehaviorsTaskCompletionSource.TrySetCanceled();

        _logger.LogTrace("{sequenceType} '{sequenceId}' disposed.", () => [GetType().Name, SequenceId]);

        Context.Dispose();
    }

    /// <summary>
    ///     Called when the timout is elapsed. If not overridden in a derived class, the default implementation
    ///     aborts the sequence.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    protected virtual Task OnTimeoutElapsedAsync() => AbortAsync(SequenceAbortReason.IncompleteSequence);

    private void CompleteLinkedSequence(ISequenceImplementation sequence)
    {
        sequence.NotifyProcessingCompleted();

        if (_identifiersTracker == null)
        {
            _sequences?.Remove(sequence);
            sequence.Dispose();
        }
    }

    private bool IsTimeoutElapsed() => DateTime.UtcNow > _timeoutExpiration;

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception logged")]
    private async ValueTask<bool> EnforceTimeoutAsync()
    {
        if (!IsTimeoutElapsed())
            return false;

        try
        {
            _logger.LogTrace(
                "Timeout was elapsed for {sequenceType} '{sequenceId}', executing timeout logic...",
                () => [GetType().Name, SequenceId]);
            await OnTimeoutElapsedAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogSequenceTimeoutError(this, ex);
        }

        return true;
    }

    private async ValueTask CompleteCoreAsync(CancellationToken cancellationToken = default)
    {
        if (!IsPending)
            return;

        _logger.LogTrace(
            "Completing {sequenceType} '{sequenceId}' (length {sequenceLength})...",
            () => [GetType().Name, SequenceId, Length]);

        IsCompleting = true;

        try
        {
            await _streamProvider.CompleteAsync(cancellationToken).ConfigureAwait(false);
            await _addCancellationTokenSource.CancelAsync().ConfigureAwait(false);
            _completeState = CompleteState.Complete;
        }
        finally
        {
            IsCompleting = false;
        }
    }

    private void ResetTimeout()
    {
        if (!_enforceTimeout || !IsPending)
            return;

        _timeoutExpiration = DateTime.UtcNow.Add(_timeout);
    }

    private void InitTimeoutTimer()
    {
        ResetTimeout();

        Task.Run(async () =>
            {
                int interval = (int)Math.Min(_timeout.TotalMilliseconds, 1000);

                while (IsPending)
                {
                    await Task.Delay(interval).ConfigureAwait(false);

                    if (IsPending && !IsCompleting)
                        await EnforceTimeoutAsync().ConfigureAwait(false);
                }
            })
            .FireAndForget();
    }

    private async Task AbortCoreAsync(SequenceAbortReason reason, Exception? exception)
    {
        if (IsAborted)
        {
            // Multiple calls to AbortAsync should wait until the sequence is aborted for real; otherwise the TransactionHandlerConsumerBehavior
            // could continue before the abort is done, preventing the error policies to be correctly and successfully applied.
            await _abortingTaskCompletionSource!.Task.ConfigureAwait(false);
            return;
        }

        _completeState = CompleteState.Aborted;
        _abortingTaskCompletionSource = new TaskCompletionSource<bool>();

        if (reason > AbortReason)
        {
            AbortReason = reason;
            AbortException = exception;
        }

        _logger.LogTrace(
            AbortException,
            "Aborting {sequenceType} '{sequenceId}' ({abortReason})...",
            () => [GetType().Name, SequenceId, AbortReason]);

        // Abort pending processing (if still pending) and wait for AddAsync to gracefully complete, ensuring that the identifiers
        // are properly tracked in all cases (enumeration aborted included)
        _streamProvider.AbortIfPending();
        _logger.LogTrace("Aborting {sequenceType} '{sequenceId}' is waiting for add semaphore...", () => [GetType().Name, SequenceId]);
        await _addSemaphoreSlim.WaitAsync().ConfigureAwait(false);

        try
        {
            await Context.SequenceStore.RemoveAsync(SequenceId).ConfigureAwait(false);
            if (await RollbackTransactionAndNotifyProcessingCompletedAsync(exception).ConfigureAwait(false))
                LogAbort();
        }
        finally
        {
            _addSemaphoreSlim.Release();
        }

        await _addCancellationTokenSource.CancelAsync().ConfigureAwait(false);
        _abortingTaskCompletionSource?.SetResult(true);
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception notified")]
    private async Task<bool> RollbackTransactionAndNotifyProcessingCompletedAsync(Exception? exception)
    {
        bool done = true;

        try
        {
            switch (AbortReason)
            {
                case SequenceAbortReason.Error:
                    if (!await ErrorPoliciesHelper.ApplyErrorPoliciesAsync(Context, exception!).ConfigureAwait(false))
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

    private async Task<bool> WaitIfNotDisposedAsync(SemaphoreSlim semaphore)
    {
        if (_isDisposed)
        {
            _logger.LogTrace("Sequence is disposed, no need to wait for semaphore");
            return false;
        }

        try
        {
            _logger.LogTrace("Waiting for semaphore...");
            await semaphore.WaitAsync().ConfigureAwait(false);
            _logger.LogTrace("Semaphore acquired");
            return true;
        }
        catch (ObjectDisposedException)
        {
            _logger.LogTrace("Object disposed exception while waiting for semaphore");
            return false;
        }
    }
}
