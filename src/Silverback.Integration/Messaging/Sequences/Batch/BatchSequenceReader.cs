// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Util;

namespace Silverback.Messaging.Sequences.Batch;

/// <summary>
///     Enables the batch processing creating a <see cref="BatchSequence" /> containing the configured number
///     of messages.
/// </summary>
public sealed class BatchSequenceReader : SequenceReaderBase, ISorted
{
    private const string SequenceIdPrefix = "batch-";

    /// <inheritdoc cref="ISorted.SortIndex" />
    public int SortIndex => int.MaxValue; // Ignored if a proper sequence is detected

    /// <inheritdoc cref="SequenceReaderBase.CanHandleAsync" />
    public override ValueTask<bool> CanHandleAsync(ConsumerPipelineContext context)
    {
        Check.NotNull(context, nameof(context));

        bool isBatchEnabled = context.Envelope.Endpoint.Configuration.Batch != null;

        return ValueTaskFactory.FromResult(isBatchEnabled);
    }

    /// <inheritdoc cref="SequenceReaderBase.GetSequenceIdAsync" />
    protected override ValueTask<string> GetSequenceIdAsync(ConsumerPipelineContext context) =>
        ValueTaskFactory.FromResult(SequenceIdPrefix);

    /// <inheritdoc cref="SequenceReaderBase.IsNewSequenceAsync" />
    protected override async ValueTask<bool> IsNewSequenceAsync(string sequenceId, ConsumerPipelineContext context)
    {
        Check.NotNull(context, nameof(context));

        BatchSequence? currentSequence = await context.SequenceStore.GetAsync<BatchSequence>(sequenceId, true)
            .ConfigureAwait(false);

        return currentSequence == null || !currentSequence.IsPending || currentSequence.IsCompleting;
    }

    /// <inheritdoc cref="SequenceReaderBase.CreateNewSequenceCore" />
    protected override ISequence CreateNewSequenceCore(string sequenceId, ConsumerPipelineContext context) =>
        new BatchSequence(sequenceId + Guid.NewGuid().ToString("N"), context);

    /// <inheritdoc cref="SequenceReaderBase.GetExistingSequenceAsync" />
    protected override ValueTask<ISequence?> GetExistingSequenceAsync(ConsumerPipelineContext context, string sequenceId)
    {
        Check.NotNull(context, nameof(context));

        return context.SequenceStore.GetAsync<ISequence>(sequenceId, true);
    }
}
