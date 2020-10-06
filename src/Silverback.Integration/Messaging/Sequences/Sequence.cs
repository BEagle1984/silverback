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
    public abstract class Sequence : ISequence
    {
        private readonly ISequenceStore _store;

        private readonly List<IOffset> _offsets = new List<IOffset>();

        private readonly MessageStreamProvider<IRawInboundEnvelope> _streamProvider;

        private readonly CancellationTokenSource _abortCancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        ///     Initializes a new instance of the <see cref="Sequence" /> class.
        /// </summary>
        /// <param name="sequenceId">
        ///     The identifier that is used to match the consumed messages with their belonging sequence.
        /// </param>
        /// <param name="context">
        ///     The current <see cref="ConsumerPipelineContext" />, assuming that it will be the one from which the
        ///     sequence gets published to the internal bus.
        /// </param>
        /// <param name="store">
        ///     The <see cref="ISequenceStore" /> that references this sequence.
        /// </param>
        protected Sequence(object sequenceId, ConsumerPipelineContext context, ISequenceStore store)
        {
            SequenceId = Check.NotNull(sequenceId, nameof(sequenceId));
            Context = Check.NotNull(context, nameof(context));
            _store = Check.NotNull(store, nameof(store));

            _streamProvider = new MessageStreamProvider<IRawInboundEnvelope>();
            Stream = _streamProvider.CreateStream<IRawInboundEnvelope>();
        }

        /// <inheritdoc cref="ISequence.SequenceId" />
        public object SequenceId { get; }

        /// <inheritdoc cref="ISequence.IsNew" />
        public bool IsNew { get; internal set; } = true;

        /// <inheritdoc cref="ISequence.Offsets" />
        public IReadOnlyList<IOffset> Offsets => _offsets;

        /// <inheritdoc cref="ISequence.Stream" />
        public IMessageStreamEnumerable<IRawInboundEnvelope> Stream { get; }

        /// <inheritdoc cref="ISequence.Context" />
        public ConsumerPipelineContext Context { get; }

        /// <inheritdoc cref="ISequence.Length" />
        public int Length { get; protected set; }

        /// <inheritdoc cref="ISequence.TotalLength" />
        public int? TotalLength { get; protected set; }

        /// <inheritdoc cref="ISequence.IsComplete" />
        public bool IsComplete { get; private set; }

        /// <inheritdoc cref="ISequence.IsAborted" />
        public bool IsAborted { get; private set; }

        /// <inheritdoc cref="ISequence.AbortProcessing" />
        public async Task AbortAsync()
        {
            if (IsComplete || IsAborted)
                return;

            IsAborted = true;
            await _streamProvider.AbortAsync().ConfigureAwait(false);
            await _store.RemoveAsync(SequenceId).ConfigureAwait(false);

            // TODO: Review this!!!
            _abortCancellationTokenSource.Cancel();
        }

        /// <inheritdoc cref="ISequence.AddAsync" />
        public virtual async Task AddAsync(IRawInboundEnvelope envelope)
        {
            Check.NotNull(envelope, nameof(envelope));

            if (envelope.Offset != null)
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
            IsComplete = true;
            await _streamProvider.CompleteAsync(cancellationToken).ConfigureAwait(false);
            await _store.RemoveAsync(SequenceId).ConfigureAwait(false);
        }
    }
}
