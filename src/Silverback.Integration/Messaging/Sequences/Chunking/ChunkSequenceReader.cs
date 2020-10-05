// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Sequences.Chunking
{
    public class ChunkSequenceReader : ISequenceReader
    {
        // TODO: The store should be scoped to the topic (sequenceId may not be globally unique)
        private readonly ISequenceStore<ChunkSequence> _sequenceStore;

        public ChunkSequenceReader(ISequenceStore<ChunkSequence> sequenceStore)
        {
            _sequenceStore = sequenceStore;
        }

        public bool CanHandle(ConsumerPipelineContext context)
        {
            Check.NotNull(context, nameof(context));

            return context.Envelope.Headers.Contains(DefaultMessageHeaders.ChunkIndex);
        }

        public ISequence? GetSequence(ConsumerPipelineContext context, out bool isNew)
        {
            Check.NotNull(context, nameof(context));

            var envelope = context.Envelope;

            var chunkIndex = envelope.Headers.GetValue<int>(DefaultMessageHeaders.ChunkIndex) ??
                             throw new InvalidOperationException("Chunk index header not found.");

            var messageId = envelope.Headers.GetValue(DefaultMessageHeaders.MessageId);

            if (string.IsNullOrEmpty(messageId))
                throw new InvalidOperationException("Message id header not found or invalid.");

            ChunkSequence? sequence;

            if (chunkIndex == 0)
            {
                sequence = _sequenceStore.Add(CreateNewSequence(messageId, context, _sequenceStore));

                // Replace the envelope with the stream that will be pushed with all the chunks.
                context.Envelope = context.Envelope.CloneReplacingStream(new ChunkStream(sequence.Stream));

                isNew = true;
            }
            else
            {
                sequence = _sequenceStore.Get(messageId);
                isNew = false;
            }

            // Skip the message if a sequence cannot be found. It probably means that the consumer started in the
            // middle of a sequence.
            if (sequence == null)
            {
                // TODO: Log
            }

            return sequence;
        }

        private static ChunkSequence CreateNewSequence(string messageId, ConsumerPipelineContext context, ISequenceStore store)
        {
            var chunksCount = context.Envelope.Headers.GetValue<int>(DefaultMessageHeaders.ChunksCount);

            if (chunksCount == null)
                throw new InvalidOperationException("Chunks count header not found or invalid.");

            return new ChunkSequence(messageId, chunksCount.Value, context, store);
        }
    }
}
