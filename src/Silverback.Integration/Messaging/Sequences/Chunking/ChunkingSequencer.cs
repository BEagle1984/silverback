// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Sequences
{
    public class ChunksSequenceReader : ISequenceReader
    {
        public IRawInboundEnvelope? SetSequence(IRawInboundEnvelope rawInboundEnvelope)
        {
            (int? chunkIndex, int? chunksCount, string? messageId) = ExtractHeadersValues(rawInboundEnvelope);

            if (chunkIndex == null)
                return rawInboundEnvelope;

            if ()


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

        private static (int? chunkIndex, int? chunksCount, string? messageId) ExtractHeadersValues(
            IRawInboundEnvelope envelope)
        {
            var chunkIndex = envelope.Headers.GetValue<int>(DefaultMessageHeaders.ChunkIndex);

            if (chunkIndex == null)
                return (null, null, null);

            var chunksCount = envelope.Headers.GetValue<int>(DefaultMessageHeaders.ChunksCount);
            var messageId = envelope.Headers.GetValue(DefaultMessageHeaders.MessageId);

            if (chunksCount == null)
                throw new InvalidOperationException("Chunks count header not found or invalid.");

            if (string.IsNullOrEmpty(messageId))
                throw new InvalidOperationException("Message id header not found or invalid.");

            return (chunkIndex, chunksCount, messageId);
        }
    }
}
