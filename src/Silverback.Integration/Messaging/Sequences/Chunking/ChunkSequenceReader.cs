// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Sequences.Chunking;

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
    public override ValueTask<bool> CanHandleAsync(ConsumerPipelineContext context)
    {
        Check.NotNull(context, nameof(context));

        int? chunkIndex = context.Envelope.Headers.GetValue<int>(DefaultMessageHeaders.ChunkIndex);
        int? chunksCount = context.Envelope.Headers.GetValue<int>(DefaultMessageHeaders.ChunksCount);
        bool? isLastChunk = context.Envelope.Headers.GetValue<bool>(DefaultMessageHeaders.IsLastChunk);

        // Skip chunking if the message is not chunked or it consists of a single chunk
        return ValueTask.FromResult(chunkIndex != null && chunksCount is null or > 1 && (isLastChunk != true || chunkIndex > 0));
    }

    /// <inheritdoc cref="SequenceReaderBase.IsNewSequenceAsync" />
    protected override ValueTask<bool> IsNewSequenceAsync(string sequenceId, ConsumerPipelineContext context)
    {
        Check.NotNull(context, nameof(context));

        int chunkIndex = context.Envelope.Headers.GetValue<int>(DefaultMessageHeaders.ChunkIndex) ??
                         throw new InvalidOperationException("Chunk index header not found.");

        return ValueTask.FromResult(chunkIndex == 0);
    }

    /// <inheritdoc cref="SequenceReaderBase.CreateNewSequenceCore" />
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Stream ownership is transferred to the envelope")]
    protected override ISequence CreateNewSequenceCore(string sequenceId, ConsumerPipelineContext context)
    {
        Check.NotNull(context, nameof(context));

        int? chunksCount = context.Envelope.Headers.GetValue<int>(DefaultMessageHeaders.ChunksCount);

        ChunkSequence sequence = new(sequenceId, chunksCount, context);

        // Replace the envelope with the stream that will be pushed with all the chunks.
        ChunkStream chunkStream = new(sequence.CreateStream<IRawInboundEnvelope>());
        context.Envelope = context.Envelope.CloneReplacingRawMessage(chunkStream);

        return sequence;
    }
}
