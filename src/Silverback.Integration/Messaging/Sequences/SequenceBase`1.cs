// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Inbound.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Sequences
{
    /// <inheritdoc cref="ISequence" />
    public abstract class SequenceBase<TEnvelope> : ISequenceImplementation
        where TEnvelope : IRawInboundEnvelope
    {
        private readonly MessageStreamProvider<TEnvelope> _streamProvider;

        private readonly List<IOffset> _offsets = new List<IOffset>();

        private readonly bool _enforceTimeout;

        private readonly TimeSpan _timeout;

        private readonly CancellationTokenSource _abortCancellationTokenSource = new CancellationTokenSource();

        private readonly object _abortLockObject = new object();

        private readonly ISilverbackIntegrationLogger<SequenceBase<TEnvelope>> _logger;

        private readonly TaskCompletionSource<bool> _sequencerBehaviorsTaskCompletionSource =
            new TaskCompletionSource<bool>();

        private readonly TaskCompletionSource<bool> _processingCompleteTaskCompletionSource =
            new TaskCompletionSource<bool>();

        private TaskCompletionSource<bool>? _abortingTaskCompletionSource;

        private CancellationTokenSource? _timeoutCancellationTokenSource;

        private ICollection<ISequence>? _sequences;

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
        ///     A value indicating whether the timeout has to be enforced.
        /// </param>
        /// <param name="timeout">
        ///     The timeout to be applied. If not specified the value of <c>Endpoint.Sequence.Timeout</c> will be
        ///     used.
        /// </param>
        /// <param name="streamProvider">
        ///     The <see cref="IMessageStreamProvider" /> to be pushed. A new one will be created if not provided.
        /// </param>
        protected SequenceBase(
            string sequenceId,
            ConsumerPipelineContext context,
            bool enforceTimeout = true,
            TimeSpan? timeout = null,
            IMessageStreamProvider? streamProvider = null)
        {
            SequenceId = Check.NotNull(sequenceId, nameof(sequenceId));
            Context = Check.NotNull(context, nameof(context));

            _streamProvider = streamProvider as MessageStreamProvider<TEnvelope> ??
                              new MessageStreamProvider<TEnvelope>();

            _logger =
                context.ServiceProvider.GetRequiredService<ISilverbackIntegrationLogger<SequenceBase<TEnvelope>>>();

            _enforceTimeout = enforceTimeout;
            _timeout = timeout ?? Context.Envelope.Endpoint.Sequence.Timeout;
            ResetTimeout();
        }

        /// <inheritdoc cref="ISequence.SequenceId" />
        public string SequenceId { get; }

        /// <inheritdoc cref="ISequence.IsPending" />
        public bool IsPending => !IsComplete && !IsAborted;

        /// <inheritdoc cref="ISequence.IsAborted" />
        public bool IsAborted => AbortReason != SequenceAbortReason.None;

        /// <inheritdoc cref="ISequence.IsBeingConsumed" />
        public bool IsBeingConsumed => _streamProvider.StreamsCount > 0;

        /// <inheritdoc cref="ISequence.Offsets" />
        public IReadOnlyList<IOffset> Offsets =>
            _sequences != null
                ? _offsets.Union(_sequences.SelectMany(sequence => sequence.Offsets)).ToList()
                : _offsets;

        /// <inheritdoc cref="ISequence.Sequences" />
        public IReadOnlyCollection<ISequence> Sequences =>
            (IReadOnlyCollection<ISequence>?)_sequences ?? Array.Empty<ISequence>();

        /// <inheritdoc cref="ISequence.Context" />
        public ConsumerPipelineContext Context { get; }

        /// <inheritdoc cref="ISequenceImplementation.SequencerBehaviorsTask" />
        public Task SequencerBehaviorsTask => _sequencerBehaviorsTaskCompletionSource.Task;

        /// <inheritdoc cref="ISequenceImplementation.ProcessingCompletedTask" />
        public Task ProcessingCompletedTask => _processingCompleteTaskCompletionSource.Task;

        /// <inheritdoc cref="ISequence.StreamProvider" />
        public IMessageStreamProvider StreamProvider => _streamProvider;

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

        /// <inheritdoc cref="ISequence.CreateStream{TMessage}" />
        public IMessageStreamEnumerable<TMessage> CreateStream<TMessage>() => StreamProvider.CreateStream<TMessage>();

        /// <inheritdoc cref="ISequence.AddAsync" />
        public Task<int> AddAsync(IRawInboundEnvelope envelope, ISequence? sequence, bool throwIfUnhandled = true)
        {
            Check.NotNull(envelope, nameof(envelope));

            if (!(envelope is TEnvelope typedEnvelope))
                throw new ArgumentException($"Expected an envelope of type {typeof(TEnvelope).Name}.");

            return AddCoreAsync(typedEnvelope, sequence, throwIfUnhandled);
        }

        /// <inheritdoc cref="ISequence.AbortAsync" />
        public async Task AbortAsync(SequenceAbortReason reason, Exception? exception = null)
        {
            // Prevent aborting a completed or already aborted sequence while disconnecting/disposing
            if ((reason == SequenceAbortReason.Disposing || reason == SequenceAbortReason.ConsumerAborted) &&
                !IsPending)
                return;

            if (reason == SequenceAbortReason.None)
                throw new ArgumentOutOfRangeException(nameof(reason), reason, "Reason not specified.");

            if (reason == SequenceAbortReason.Error && exception == null)
            {
                throw new ArgumentNullException(
                    nameof(exception),
                    "The exception must be specified if the reason is Error.");
            }

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

            _timeoutCancellationTokenSource?.Cancel();

            _logger.LogSequenceAborted(Context.Envelope, this, AbortReason, AbortException);

            await Context.SequenceStore.RemoveAsync(SequenceId).ConfigureAwait(false);
            await HandleExceptionAsync(exception).ConfigureAwait(false);
            _streamProvider.Abort();

            _abortCancellationTokenSource.Cancel();
            _abortingTaskCompletionSource?.SetResult(true);
        }

        /// <inheritdoc cref="ISequenceImplementation.SetIsNew" />
        void ISequenceImplementation.SetIsNew(bool value) => IsNew = value;

        /// <inheritdoc cref="ISequenceImplementation.SetIsNew" />
        void ISequenceImplementation.CompleteSequencerBehaviorsTask() =>
            _sequencerBehaviorsTaskCompletionSource.TrySetResult(true);

        /// <inheritdoc cref="ISequenceImplementation.NotifyProcessingCompleted" />
        void ISequenceImplementation.NotifyProcessingCompleted()
        {
            _processingCompleteTaskCompletionSource.TrySetResult(true);
            _sequences?.OfType<ISequenceImplementation>().ForEach(sequence => sequence.NotifyProcessingCompleted());
        }

        /// <inheritdoc cref="ISequenceImplementation.NotifyProcessingFailed" />
        void ISequenceImplementation.NotifyProcessingFailed(Exception exception)
        {
            _processingCompleteTaskCompletionSource.TrySetException(exception);

            // Don't forward the error, it's enough to handle it once
            _sequences?.OfType<ISequenceImplementation>().ForEach(sequence => sequence.NotifyProcessingCompleted());
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
        ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains the
        ///     number of streams that have been pushed.
        /// </returns>
        protected virtual async Task<int> AddCoreAsync(TEnvelope envelope, ISequence? sequence, bool throwIfUnhandled)
        {
            ResetTimeout();

            if (sequence != null && sequence != this)
            {
                _sequences ??= new List<ISequence>();
                _sequences.Add(sequence);
            }
            else
            {
                _offsets.Add(envelope.Offset);
            }

            try
            {
                if (TotalLength != null && Length > TotalLength)
                {
                    // TODO: Log? / Throw?
                    return 0;
                }

                _abortCancellationTokenSource.Token.ThrowIfCancellationRequested();

                Length++;

                if (TotalLength != null && Length == TotalLength || IsLastMessage(envelope))
                {
                    TotalLength = Length;
                    IsCompleting = true;
                }

                int pushedStreamsCount = await _streamProvider.PushAsync(
                        envelope,
                        throwIfUnhandled,
                        _abortCancellationTokenSource.Token)
                    .ConfigureAwait(false);

                if (IsCompleting)
                    await CompleteAsync().ConfigureAwait(false);

                return pushedStreamsCount;
            }
            catch (OperationCanceledException)
            {
                // Ignore

                // TODO: Is it correct to ignore?

                return 0;
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
        protected virtual async Task CompleteAsync(CancellationToken cancellationToken = default)
        {
            if (!IsPending)
                return;

            IsComplete = true;
            IsCompleting = false;

            _timeoutCancellationTokenSource?.Cancel();

            await Context.SequenceStore.RemoveAsync(SequenceId).ConfigureAwait(false);
            await _streamProvider.CompleteAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged
        ///     resources.
        /// </summary>
        /// <param name="disposing">
        ///     A value indicating whether the method has been called by the <c>Dispose</c> method and not from the
        ///     finalizer.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            // TODO: Ensure Dispose is actually called
            if (disposing)
            {
                if (_abortingTaskCompletionSource != null)
                    _abortingTaskCompletionSource.Task.Wait();

                _streamProvider.Dispose();
                _abortCancellationTokenSource.Dispose();
                _timeoutCancellationTokenSource?.Cancel();
                _timeoutCancellationTokenSource?.Dispose();
                _timeoutCancellationTokenSource = null;

                _sequences?.ForEach(sequence => sequence.Dispose());

                Context.Dispose();
            }
        }

        /// <summary>
        ///     Called when the timout is elapsed. If not overridden in a derived class, the default implementation
        ///     aborts the sequence.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        protected virtual Task OnTimeoutElapsedAsync() => AbortAsync(SequenceAbortReason.IncompleteSequence);

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

            Task.Run(
                async () =>
                {
                    await Task.Delay(_timeout, _timeoutCancellationTokenSource.Token).ConfigureAwait(false);

                    try
                    {
                        await OnTimeoutElapsedAsync().ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        // Ignore
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "An error occurred aborting the timed out sequence.");
                    }
                });
        }

        [SuppressMessage("", "CA1031", Justification = "Exeption notified")]
        private async Task HandleExceptionAsync(Exception? exception)
        {
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
                            return;
                        }

                        break;
                    case SequenceAbortReason.EnumerationAborted:
                        await Context.TransactionManager.CommitAsync().ConfigureAwait(false);
                        break;
                    case SequenceAbortReason.IncompleteSequence:
                        await Context.TransactionManager.RollbackAsync(exception, true).ConfigureAwait(false);
                        break;
                    case SequenceAbortReason.None:
                        throw new InvalidOperationException("Reason shouldn't be None.");
                    case SequenceAbortReason.ConsumerAborted:
                    case SequenceAbortReason.Disposing:
                    default:
                        await Context.TransactionManager.RollbackAsync(exception).ConfigureAwait(false);

                        break;
                }

                ((ISequenceImplementation)this).NotifyProcessingCompleted();
            }
            catch (Exception newException)
            {
                ((ISequenceImplementation)this).NotifyProcessingFailed(newException!);
            }
        }
    }
}
