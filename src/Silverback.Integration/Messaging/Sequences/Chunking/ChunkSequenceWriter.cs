// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Sequences.Chunking
{
    /// <summary>
    ///     Creates the chunks sequence according to the <see cref="ChunkSettings"/>.
    /// </summary>
    public class ChunkSequenceWriter : ISequenceWriter
    {
        /// <inheritdoc cref="ISequenceWriter.CanHandle" />
        public bool CanHandle(IOutboundEnvelope envelope)
        {
            Check.NotNull(envelope, nameof(envelope));

            var chunkSettings = envelope.Endpoint.Chunk;
            if (chunkSettings == null || chunkSettings.Size == int.MaxValue)
                return false;

            if (chunkSettings.AlwaysAddHeaders)
                return true;

            if (envelope.RawMessage == null)
                return false;

            if (!envelope.RawMessage.CanSeek)
                return true;

            return envelope.RawMessage != null && envelope.RawMessage.Length > chunkSettings.Size;
        }

        /// <inheritdoc cref="ISequenceWriter.ProcessMessage" />
        [SuppressMessage("ReSharper", "ASYNC0001", Justification = "False positive")]
        public async IAsyncEnumerable<IOutboundEnvelope> ProcessMessage(IOutboundEnvelope envelope)
        {
            Check.NotNull(envelope, nameof(envelope));

            if (envelope.RawMessage == null)
                throw new InvalidOperationException("RawMessage is null");

            var settings = envelope.Endpoint.Chunk;
            var chunkSize = settings?.Size ?? int.MaxValue;
            var bufferArray = ArrayPool<byte>.Shared.Rent(chunkSize);
            var bufferMemory = bufferArray.AsMemory(0, chunkSize);

            var chunksCount = envelope.RawMessage.CanSeek
                ? (int?)Math.Ceiling(envelope.RawMessage.Length / (double)chunkSize)
                : null;

            IBrokerMessageOffset? firstChunkOffset = null;

            int chunkIndex = 0;

            var readBytesCount = await envelope.RawMessage.ReadAsync(bufferMemory).ConfigureAwait(false);

            while (readBytesCount > 0)
            {
                var chunkEnvelope = CreateChunkEnvelope(
                    chunkIndex,
                    chunksCount,
                    bufferMemory.Slice(0, readBytesCount).ToArray(),
                    envelope);

                if (chunkIndex > 0)
                    chunkEnvelope.Headers.AddOrReplace(DefaultMessageHeaders.FirstChunkOffset, firstChunkOffset?.Value);

                // Read the next chunk and check if we were at the end of the stream (to not rely on Length property)
                readBytesCount = await envelope.RawMessage.ReadAsync(bufferMemory).ConfigureAwait(false);

                if (readBytesCount == 0)
                    chunkEnvelope.Headers.AddOrReplace(DefaultMessageHeaders.IsLastChunk, true.ToString());

                yield return chunkEnvelope;

                // Read and store the offset of the first chunk, after it has been produced (after yield return)
                if (chunkIndex == 0)
                    firstChunkOffset = chunkEnvelope.BrokerMessageIdentifier as IBrokerMessageOffset;

                chunkIndex++;
            }
        }

        private static IOutboundEnvelope CreateChunkEnvelope(
            in int chunkIndex,
            in int? chunksCount,
            byte[] rawContent,
            IOutboundEnvelope originalEnvelope)
        {
            var envelope = new OutboundEnvelope(
                originalEnvelope.Message,
                originalEnvelope.Headers,
                originalEnvelope.Endpoint,
                originalEnvelope.AutoUnwrap)
            {
                RawMessage = new MemoryStream(rawContent)
            };

            envelope.Headers.AddOrReplace(DefaultMessageHeaders.ChunkIndex, chunkIndex);

            if (chunksCount != null)
                envelope.Headers.AddOrReplace(DefaultMessageHeaders.ChunksCount, chunksCount);

            return envelope;
        }
    }
}
