// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Buffers;
using System.Collections.Generic;
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

            return envelope.RawMessage != null && envelope.RawMessage.Length > chunkSettings.Size;
        }

        /// <inheritdoc cref="ISequenceWriter.ProcessMessage" />
        public async IAsyncEnumerable<IOutboundEnvelope> ProcessMessage(IOutboundEnvelope envelope)
        {
            Check.NotNull(envelope, nameof(envelope));

            if (envelope.RawMessage == null)
                throw new InvalidOperationException("RawMessage is null");

            var settings = envelope.Endpoint.Chunk;

            var chunkSize = settings?.Size ?? int.MaxValue;

            var messageId = envelope.Headers.GetValue(DefaultMessageHeaders.MessageId);
            if (string.IsNullOrEmpty(messageId))
            {
                throw new InvalidOperationException(
                    "Dividing into chunks is pointless if no unique MessageId can be retrieved. " +
                    $"Please set the {DefaultMessageHeaders.MessageId} header.");
            }

            var bufferArray = ArrayPool<byte>.Shared.Rent(chunkSize);
            var bufferMemory = bufferArray.AsMemory(0, chunkSize);

            var chunksCount = (int)Math.Ceiling(envelope.RawMessage.Length / (double)chunkSize);

            IOffset? firstChunkOffset = null;

            for (var i = 0; i < chunksCount; i++)
            {
                var length = await envelope.RawMessage.ReadAsync(bufferMemory).ConfigureAwait(false);

                var chunkEnvelope = CreateChunkEnvelope(
                    i,
                    chunksCount,
                    bufferMemory.Slice(0, length).ToArray(),
                    envelope);

                if (i > 0)
                    chunkEnvelope.Headers.AddOrReplace(DefaultMessageHeaders.FirstChunkOffset, firstChunkOffset?.Value);

                yield return chunkEnvelope;

                if (i == 0)
                    firstChunkOffset = chunkEnvelope.Offset;
            }
        }

        private static IOutboundEnvelope CreateChunkEnvelope(
            in int chunkIndex,
            in int chunksCount,
            byte[] rawContent,
            IOutboundEnvelope originalEnvelope)
        {
            var messageChunk = new OutboundEnvelope(
                originalEnvelope.Message,
                originalEnvelope.Headers,
                originalEnvelope.Endpoint,
                (originalEnvelope as IOutboundEnvelopeInternal)?.OutboundConnectorType,
                originalEnvelope.AutoUnwrap)
            {
                RawMessage = new MemoryStream(rawContent)
            };

            messageChunk.Headers.AddOrReplace(DefaultMessageHeaders.ChunkIndex, chunkIndex);
            messageChunk.Headers.AddOrReplace(DefaultMessageHeaders.ChunksCount, chunksCount);

            return messageChunk;
        }
    }
}
