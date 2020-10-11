// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Sequences
{
    /// <inheritdoc cref="ISequence" />
    public abstract class Sequence<TEnvelope> : ISequenceImplementation
        where TEnvelope : IRawInboundEnvelope
    {
        private readonly MessageStreamProvider<TEnvelope> _streamProvider;

        private readonly List<IOffset> _offsets = new List<IOffset>();

        private readonly bool _enforceTimeout;

        private readonly TimeSpan _timeout;

        private readonly CancellationTokenSource _abortCancellationTokenSource = new CancellationTokenSource();

        private readonly object _abortLockObject = new object();

        private CancellationTokenSource? _timeoutCancellationTokenSource;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Sequence{TEnvelope}" /> class.
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
        protected Sequence(
            string sequenceId,
            ConsumerPipelineContext context,
            bool enforceTimeout = true,
            TimeSpan? timeout = null)
        {
            SequenceId = Check.NotNull(sequenceId, nameof(sequenceId));
            Context = Check.NotNull(context, nameof(context));

            _streamProvider = new MessageStreamProvider<TEnvelope>();

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

        /// <inheritdoc cref="ISequence.Offsets" />
        public IReadOnlyList<IOffset> Offsets => _offsets;

        /// <inheritdoc cref="ISequence.Context" />
        public ConsumerPipelineContext Context { get; }

        /// <inheritdoc cref="ISequence.ProcessedTaskCompletionSource" />
        public TaskCompletionSource<bool> ProcessedTaskCompletionSource { get; } = new TaskCompletionSource<bool>();

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

        /// <inheritdoc cref="ISequence.IsComplete" />
        public bool IsComplete { get; private set; }

        /// <inheritdoc cref="ISequence.AbortReason" />
        public SequenceAbortReason AbortReason { get; private set; }

        /// <inheritdoc cref="ISequence.CreateStream{TMessage}" />
        public IMessageStreamEnumerable<TMessage> CreateStream<TMessage>() => StreamProvider.CreateStream<TMessage>();

        /// <inheritdoc cref="ISequence.AddAsync" />
        public Task AddAsync(IRawInboundEnvelope envelope)
        {
            Check.NotNull(envelope, nameof(envelope));

            if (!(envelope is TEnvelope typedEnvelope))
                throw new ArgumentException($"Expected an envelope of type {typeof(TEnvelope).Name}.");

            return AddCoreAsync(typedEnvelope);
        }

        /// <inheritdoc cref="ISequence.AbortAsync" />
        public async Task AbortAsync(SequenceAbortReason reason, Exception? exception = null)
        {
            if (reason == SequenceAbortReason.None)
                throw new ArgumentOutOfRangeException(nameof(reason), reason, "Reason not specified.");

            if (reason == SequenceAbortReason.Error && exception == null)
            {
                throw new ArgumentNullException(
                    nameof(exception),
                    "The exception must be specified if the reason is Error.");
            }

            lock (_abortLockObject)
            {
                if (!IsPending)
                    return;

                if (reason > AbortReason)
                {
                    AbortReason = reason;
                    AbortException = exception;
                }
            }

            _timeoutCancellationTokenSource?.Cancel();

            await Context.SequenceStore.RemoveAsync(SequenceId).ConfigureAwait(false);
            await _streamProvider.AbortAsync().ConfigureAwait(false);

            // TODO: Review this!!!
            _abortCancellationTokenSource.Cancel();
        }

        /// <inheritdoc cref="IDisposable.Dispose" />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc cref="ISequence.AddAsync" />
        protected virtual async Task AddCoreAsync(TEnvelope envelope)
        {
            ResetTimeout();

            _offsets.Add(envelope.Offset);

            try
            {
                if (TotalLength != null && Length > TotalLength)
                {
                    // TODO: Log? / Throw?
                    return;
                }

                Length++;

                if (!_abortCancellationTokenSource.IsCancellationRequested)
                {
                    await _streamProvider.PushAsync(envelope, _abortCancellationTokenSource.Token)
                        .ConfigureAwait(false);
                }

                if (TotalLength != null && Length == TotalLength)
                    await CompleteAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Ignore

                // TODO: Is it correct to ignore?
            }
        }

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

            _timeoutCancellationTokenSource?.Cancel();

            await _streamProvider.CompleteAsync(cancellationToken).ConfigureAwait(false);
            await Context.SequenceStore.RemoveAsync(SequenceId).ConfigureAwait(false);
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
                _streamProvider.Dispose();
                _abortCancellationTokenSource.Dispose();
                _timeoutCancellationTokenSource?.Cancel();
                _timeoutCancellationTokenSource?.Dispose();
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

        /// <inheritdoc cref="ISequenceImplementation.SetIsNew" />
        void ISequenceImplementation.SetIsNew(bool value) => IsNew = value;

        private void ResetTimeout()
        {
            if (!_enforceTimeout)
                return;

            _timeoutCancellationTokenSource?.Cancel();
            _timeoutCancellationTokenSource = new CancellationTokenSource();

            Task.Run(
                async () =>
                {
                    try
                    {
                        await Task.Delay(_timeout, _timeoutCancellationTokenSource.Token).ConfigureAwait(false);
                        await OnTimeoutElapsedAsync().ConfigureAwait(false);
                    }
                    catch (OperationCanceledException ex)
                    {
                        // Ignore
                    }
                    catch (Exception ex)
                    {
                        // TODO: Log
                    }
                });
        }
    }
}
