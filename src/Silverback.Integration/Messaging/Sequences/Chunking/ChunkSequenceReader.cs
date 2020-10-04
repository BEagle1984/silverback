// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
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

        public bool CanHandle(IRawInboundEnvelope envelope)
        {
            Check.NotNull(envelope, nameof(envelope));

            return envelope.Headers.Contains(DefaultMessageHeaders.ChunkIndex);
        }

        public ISequence? GetSequence(IRawInboundEnvelope envelope, out bool isNew)
        {
            Check.NotNull(envelope, nameof(envelope));

            var chunkIndex = envelope.Headers.GetValue<int>(DefaultMessageHeaders.ChunkIndex) ??
                             throw new InvalidOperationException("Chunk index header not found.");

            var messageId = envelope.Headers.GetValue(DefaultMessageHeaders.MessageId);

            if (string.IsNullOrEmpty(messageId))
                throw new InvalidOperationException("Message id header not found or invalid.");

            ChunkSequence? sequence;

            if (chunkIndex == 0)
            {
                sequence = _sequenceStore.Add(CreateNewSequence(messageId, envelope));
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

        private static ChunkSequence CreateNewSequence(string messageId, IRawInboundEnvelope envelope)
        {
            var chunksCount = envelope.Headers.GetValue<int>(DefaultMessageHeaders.ChunksCount);

            if (chunksCount == null)
                throw new InvalidOperationException("Chunks count header not found or invalid.");

            return new ChunkSequence(messageId, chunksCount.Value);
        }
    }
}
