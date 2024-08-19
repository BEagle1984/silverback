// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
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
    {
        _chunkEnricherFactory = Check.NotNull(chunkEnricherFactory, nameof(chunkEnricherFactory));
        _serviceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
    }

    /// <inheritdoc cref="ISequenceWriter.CanHandle" />
    public bool CanHandle(IOutboundEnvelope envelope)
    {
        Check.NotNull(envelope, nameof(envelope));

        ChunkSettings? chunkSettings = envelope.Endpoint.Configuration.Chunk;
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
        Check.NotNull(envelope, nameof(envelope));

        if (envelope.RawMessage == null)
            throw new InvalidOperationException("RawMessage is null");

        ChunkSettings? settings = envelope.Endpoint.Configuration.Chunk;
        int chunkSize = settings?.Size ?? int.MaxValue;
        byte[] bufferArray = ArrayPool<byte>.Shared.Rent(chunkSize);
        Memory<byte> bufferMemory = bufferArray.AsMemory(0, chunkSize);

        int? chunksCount = envelope.RawMessage.CanSeek
            ? (int?)Math.Ceiling(envelope.RawMessage.Length / (double)chunkSize)
            : null;

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
                    .GetEnricher(chunkEnvelope.Endpoint, _serviceProvider)
                    .GetFirstChunkMessageHeader(chunkEnvelope);
            }

            chunkIndex++;
        }
    }

    private static IOutboundEnvelope CreateChunkEnvelope(
        in int chunkIndex,
        in int? chunksCount,
        byte[] rawContent,
        IOutboundEnvelope originalEnvelope)
    {
        IOutboundEnvelope envelope = originalEnvelope.CloneReplacingRawMessage(new MemoryStream(rawContent));

        envelope.Headers.AddOrReplace(DefaultMessageHeaders.ChunkIndex, chunkIndex);

        if (chunksCount != null)
            envelope.Headers.AddOrReplace(DefaultMessageHeaders.ChunksCount, chunksCount);

        return envelope;
    }
}
