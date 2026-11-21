// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Sequences.Chunking;

/// <summary>
///     Creates the chunks sequence according to the <see cref="ChunkSettings" />.
/// </summary>
public class ChunkSequenceWriter : ISequenceWriter
{
    private readonly IServiceProvider _serviceProvider;

    private readonly IChunkEnricherFactory _chunkEnricherFactory;

    private readonly ArrayPool<byte> _arrayPool;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ChunkSequenceWriter" /> class.
    /// </summary>
    /// <param name="chunkEnricherFactory">
    ///     The <see cref="IChunkEnricherFactory" />.
    /// </param>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" />.
    /// </param>
    public ChunkSequenceWriter(IChunkEnricherFactory chunkEnricherFactory, IServiceProvider serviceProvider)
        : this(chunkEnricherFactory, serviceProvider, ArrayPool<byte>.Shared)
    {
    }

    internal ChunkSequenceWriter(IChunkEnricherFactory chunkEnricherFactory, IServiceProvider serviceProvider, ArrayPool<byte> arrayPool)
    {
        _chunkEnricherFactory = Check.NotNull(chunkEnricherFactory);
        _serviceProvider = Check.NotNull(serviceProvider);
        _arrayPool = Check.NotNull(arrayPool);
    }

    /// <inheritdoc cref="ISequenceWriter.CanHandle" />
    public bool CanHandle(IOutboundEnvelope envelope)
    {
        Check.NotNull(envelope);

        ChunkSettings? chunkSettings = envelope.EndpointConfiguration.Chunk;
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

    /// <inheritdoc cref="ISequenceWriter.ProcessMessageAsync" />
    public async IAsyncEnumerable<IOutboundEnvelope> ProcessMessageAsync(IOutboundEnvelope envelope)
    {
        Check.NotNull(envelope);

        if (envelope.RawMessage == null)
            throw new InvalidOperationException("RawMessage is null");

        // Ensure that chunk message id and message key (as kafka key) are set to ensure that the chunks can be correlated and will land on
        // the same Kafka partition
        string messageId = envelope.Headers.GetValue(DefaultMessageHeaders.MessageKey) ?? Guid.NewGuid().ToString("N");
        envelope.AddHeaderIfNotExists(DefaultMessageHeaders.MessageKey, messageId);

        ChunkSettings? settings = envelope.EndpointConfiguration.Chunk;
        int chunkSize = settings?.Size ?? int.MaxValue;

        int? chunksCount = envelope.RawMessage.CanSeek
            ? (int?)Math.Ceiling(Math.Max(0, envelope.RawMessage.Length - envelope.RawMessage.Position) / (double)chunkSize)
            : null;

        // Don't rent more than the message length, to avoid keeping chunk-sized arrays in the pool for small messages (at least 1 byte,
        // since reading into an empty buffer would be taken as the end of the stream)
        int bufferSize = envelope.RawMessage.CanSeek
            ? (int)Math.Max(1, Math.Min(chunkSize, envelope.RawMessage.Length - envelope.RawMessage.Position))
            : chunkSize;

        // The buffer never escapes: each chunk envelope gets a copy of the read bytes. Non-seekable streams (e.g. encrypted messages)
        // always rent the full chunk size, so with large chunk sizes the shared pool retains up to one such array per concurrent producer
        // until it trims its idle arrays on a Gen2 GC. This is accepted, since it avoids a large object heap allocation per message.
        byte[] bufferArray = _arrayPool.Rent(bufferSize);

        try
        {
            Memory<byte> bufferMemory = bufferArray.AsMemory(0, bufferSize);

            MessageHeader? firstChunkMessageHeader = null;

            int chunkIndex = 0;

            int readBytesCount = await envelope.RawMessage.ReadAsync(bufferMemory).ConfigureAwait(false);

            while (readBytesCount > 0)
            {
                IOutboundEnvelope chunkEnvelope = CreateChunkEnvelope(
                    chunkIndex,
                    chunksCount,
                    bufferMemory[..readBytesCount].ToArray(),
                    envelope);

                if (chunkIndex > 0 && firstChunkMessageHeader != null)
                    chunkEnvelope.Headers.Add(firstChunkMessageHeader);

                // Read the next chunk and check if we were at the end of the stream (to not rely on Length property)
                readBytesCount = await envelope.RawMessage.ReadAsync(bufferMemory).ConfigureAwait(false);

                if (readBytesCount == 0)
                    chunkEnvelope.Headers.AddOrReplace(DefaultMessageHeaders.IsLastChunk, true.ToString());

                yield return chunkEnvelope;

                // Read and store the offset of the first chunk, after it has been produced (after yield return)
                if (chunkIndex == 0)
                {
                    firstChunkMessageHeader = _chunkEnricherFactory
                        .GetEnricher(chunkEnvelope.EndpointConfiguration, _serviceProvider)
                        .GetFirstChunkMessageHeader(chunkEnvelope);
                }

                chunkIndex++;
            }
        }
        finally
        {
            _arrayPool.Return(bufferArray);
        }
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Stream ownership is transferred to the envelope")]
    private static IOutboundEnvelope CreateChunkEnvelope(
        in int chunkIndex,
        in int? chunksCount,
        byte[] rawContent,
        IOutboundEnvelope originalEnvelope)
    {
        // The chunk content is a fresh copy that nobody else references, so the producer can use it without copying it again
        IOutboundEnvelope envelope = originalEnvelope.CloneReplacingRawMessage(new OwnedReadOnlyMemoryStream(rawContent));

        envelope.Headers.AddOrReplace(DefaultMessageHeaders.ChunkIndex, chunkIndex);

        if (chunksCount != null)
            envelope.Headers.AddOrReplace(DefaultMessageHeaders.ChunksCount, chunksCount);

        return envelope;
    }
}
