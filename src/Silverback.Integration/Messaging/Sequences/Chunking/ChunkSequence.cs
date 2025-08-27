// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Sequences.Chunking;

/// <summary>
///     A sequence of chunks that belong to the same message.
/// </summary>
public class ChunkSequence : Sequence
{
    private int? _lastIndex;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ChunkSequence" /> class.
    /// </summary>
    /// <param name="sequenceId">
    ///     The identifier that is used to match the consumed messages with their belonging sequence.
    /// </param>
    /// <param name="totalLength">
    ///     The expected total length of the sequence.
    /// </param>
    /// <param name="context">
    ///     The current <see cref="ConsumerPipelineContext" />, assuming that it will be the one from which the
    ///     sequence gets published via the message bus.
    /// </param>
    public ChunkSequence(string sequenceId, int? totalLength, ConsumerPipelineContext context)
        : base(sequenceId, context)
    {
        TotalLength = totalLength;
    }

    /// <inheritdoc cref="Sequence.IsRawMessages" />
    public override bool IsRawMessages => true;

    /// <inheritdoc cref="Sequence.AddAsync" />
    public override ValueTask<AddToSequenceResult> AddAsync(IInboundEnvelope envelope, ISequence? sequence, bool throwIfUnhandled)
    {
        Check.NotNull(envelope, nameof(envelope));

        int chunkIndex = envelope.Headers.GetValue<int>(DefaultMessageHeaders.ChunkIndex) ??
                         throw new InvalidOperationException("Chunk index header not found.");

        if (!EnsureOrdering(chunkIndex))
            return ValueTask.FromResult(AddToSequenceResult.Success(0));

        return base.AddAsync(envelope, sequence, throwIfUnhandled);
    }

    /// <inheritdoc cref="Sequence.IsLastMessage" />
    protected override bool IsLastMessage(IInboundEnvelope envelope)
    {
        Check.NotNull(envelope, nameof(envelope));

        return envelope.Headers.GetValue<bool>(DefaultMessageHeaders.IsLastChunk) == true;
    }

    private bool EnsureOrdering(int index)
    {
        if (_lastIndex == null && index != 0)
        {
            throw new SequenceException(
                $"Sequence error. Received chunk with index {index} as first " +
                $"chunk for the sequence {SequenceId}, expected index 0. ");
        }

        if (_lastIndex != null && index == _lastIndex)
            return false;

        if (_lastIndex != null && index != _lastIndex + 1)
        {
            throw new SequenceException(
                $"Sequence error. Received chunk with index {index} after index " +
                $"{_lastIndex}.");
        }

        _lastIndex = index;

        return true;
    }
}
