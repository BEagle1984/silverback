// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Sequences
{
    public class ChunksSequenceReader : ISequenceReader
    {
        // TODO: The store should be scoped to the topic (sequenceId may not be globally unique)
        private readonly ISequenceStore<ChunksSequence> _sequenceStore;

        public ChunksSequenceReader(ISequenceStore<ChunksSequence> sequenceStore)
        {
            _sequenceStore = sequenceStore;
        }

        public bool CanHandleSequence(IRawInboundEnvelope envelope)
        {
            Check.NotNull(envelope, nameof(envelope));

            return envelope.Headers.Contains(DefaultMessageHeaders.ChunkIndex);
        }

        // TODO: Custom exception type instead of InvalidOperationException?
        public async Task<ISequence?> HandleSequence(IRawInboundEnvelope envelope)
        {
            Check.NotNull(envelope, nameof(envelope));

            var chunkIndex = envelope.Headers.GetValue<int>(DefaultMessageHeaders.ChunkIndex);

            if (chunkIndex == null)
                return null;

            var messageId = envelope.Headers.GetValue(DefaultMessageHeaders.MessageId);

            if (string.IsNullOrEmpty(messageId))
                throw new InvalidOperationException("Message id header not found or invalid.");

            var sequence = chunkIndex == 0
                ? _sequenceStore.Get(messageId)
                : _sequenceStore.Add(CreateNewSequence(messageId, envelope));

            // Skip the message if a sequence cannot be found. It probably means that the consumer started in the
            // middle of a sequence.
            if (sequence == null)
                return null;

            await sequence.AddAsync(chunkIndex.Value, envelope).ConfigureAwait(false);

            return chunkIndex == 0 ? sequence : null;

            // TODO:
            // * Create ChunksSequenceReader
            //     * Read headers and set Sequence accordingly
            //     * Ensure proper order is respected
            //     * Preserve headers (last message?) and forward them!
            // * NEW
            //     * Stream envelope into sequence
            //     * Use stream<byte[]> to deserialize without blocking

            // * WAS
            //     * Determine if file: either from headers or hardcoded BinaryFileSerializer
            //     * If not a file, return null until the whole message is received and aggregated
        }

        private static ChunksSequence CreateNewSequence(string messageId, IRawInboundEnvelope envelope)
        {
            var chunksCount = envelope.Headers.GetValue<int>(DefaultMessageHeaders.ChunksCount);

            if (chunksCount == null)
                throw new InvalidOperationException("Chunks count header not found or invalid.");

            return new ChunksSequence(messageId, chunksCount.Value);
        }
    }
}
