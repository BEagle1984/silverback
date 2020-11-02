// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Sequences.Chunking
{
    /// <summary>
    ///     Creates a <see cref="ChunkSequence" /> containing all the chunks of the original message.
    /// </summary>
    public class ChunkSequenceReader : SequenceReaderBase
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ChunkSequenceReader" /> class.
        /// </summary>
        public ChunkSequenceReader()
            : base(true)
        {
        }

        /// <inheritdoc cref="SequenceReaderBase.CanHandleAsync" />
        public override Task<bool> CanHandleAsync(ConsumerPipelineContext context)
        {
            Check.NotNull(context, nameof(context));

            var canHandle = context.Envelope.Headers.Contains(DefaultMessageHeaders.ChunkIndex);

            return Task.FromResult(canHandle);
        }

        /// <inheritdoc cref="SequenceReaderBase.IsNewSequence" />
        protected override Task<bool> IsNewSequence(ConsumerPipelineContext context)
        {
            Check.NotNull(context, nameof(context));

            var chunkIndex = context.Envelope.Headers.GetValue<int>(DefaultMessageHeaders.ChunkIndex) ??
                             throw new InvalidOperationException("Chunk index header not found.");

            return Task.FromResult(chunkIndex == 0);
        }

        /// <inheritdoc cref="SequenceReaderBase.CreateNewSequenceCore" />
        protected override ISequence CreateNewSequenceCore(string sequenceId, ConsumerPipelineContext context)
        {
            Check.NotNull(context, nameof(context));

            int? chunksCount = context.Envelope.Headers.GetValue<int>(DefaultMessageHeaders.ChunksCount);

            var sequence = new ChunkSequence(sequenceId, chunksCount, context);

            // Replace the envelope with the stream that will be pushed with all the chunks.
            var chunkStream = new ChunkStream(sequence.CreateStream<IRawInboundEnvelope>());
            context.Envelope = context.Envelope.CloneReplacingStream(chunkStream);

            return sequence;
        }
    }
}
