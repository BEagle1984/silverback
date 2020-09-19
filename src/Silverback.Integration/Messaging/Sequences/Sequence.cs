// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Sequences
{
    /// <inheritdoc cref="ISequence" />
    public abstract class Sequence : ISequence
    {
        private readonly List<IOffset> _offsets = new List<IOffset>();

        private readonly MessageStreamProvider<IRawInboundEnvelope> _streamProvider;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Sequence" /> class.
        /// </summary>
        /// <param name="sequenceId">
        ///     The identifier that is used to match the consumed messages with their belonging sequence.
        /// </param>
        protected Sequence(object sequenceId)
        {
            SequenceId = sequenceId;

            _streamProvider = new MessageStreamProvider<IRawInboundEnvelope>();
            Stream = _streamProvider.CreateStream<IRawInboundEnvelope>();
        }

        /// <inheritdoc cref="ISequence.SequenceId" />
        public object SequenceId { get; }

        /// <inheritdoc cref="ISequence.Offsets" />
        public IReadOnlyList<IOffset> Offsets => _offsets;

        /// <inheritdoc cref="ISequence.Length" />
        public int Length { get; protected set; }

        /// <inheritdoc cref="ISequence.TotalLength" />
        public int? TotalLength { get; protected set; }

        /// <inheritdoc cref="ISequence.Stream" />
        public IMessageStreamEnumerable<IRawInboundEnvelope> Stream { get; }
        // TODO: Convert to Stream<byte[]>?

        /// <summary>
        ///     Adds the message to the sequence. This method should be called by the actual implementation in the
        ///     derived classes.
        /// </summary>
        /// <param name="envelope">The envelope to be added to the sequence.</param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        protected virtual async Task AddAsync(IRawInboundEnvelope envelope)
        {
            Check.NotNull(envelope, nameof(envelope));

            if (envelope.Offset != null)
                _offsets.Add(envelope.Offset);

            if (TotalLength == null || Length < TotalLength)
            {
                Length++;

                await _streamProvider.PushAsync(envelope).ConfigureAwait(false);
            }

            if (TotalLength != null && Length == TotalLength)
                await _streamProvider.CompleteAsync().ConfigureAwait(false);
        }

        /// <summary>
        ///     Marks the sequence as complete, meaning no more messages will be pushed.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        protected virtual Task CompleteAsync() => _streamProvider.CompleteAsync();
    }
}
