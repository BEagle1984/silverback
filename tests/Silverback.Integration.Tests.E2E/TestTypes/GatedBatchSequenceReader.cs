// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Sequences.Batch;

namespace Silverback.Tests.Integration.E2E.TestTypes;

// Creates the batches like BatchSequenceReader (which is sealed), but as GatedBatchSequence (see BatchTimeoutGate)
public sealed class GatedBatchSequenceReader : SequenceReaderBase, ISorted
{
    private const string SequenceIdPrefix = "batch-";

    private readonly BatchTimeoutGate _gate;

    public GatedBatchSequenceReader(BatchTimeoutGate gate)
    {
        _gate = gate;
    }

    // Before BatchSequenceReader
    public int SortIndex => int.MaxValue - 1;

    public override ValueTask<bool> CanHandleAsync(ConsumerPipelineContext context) =>
        ValueTask.FromResult(context.Envelope.Endpoint.Configuration.Batch != null);

    protected override ValueTask<string> GetSequenceIdAsync(ConsumerPipelineContext context) => ValueTask.FromResult(SequenceIdPrefix);

    protected override async ValueTask<bool> IsNewSequenceAsync(string sequenceId, ConsumerPipelineContext context)
    {
        BatchSequence? currentSequence = await context.SequenceStore.GetAsync<BatchSequence>(sequenceId, true);
        return currentSequence == null || !currentSequence.IsPending || currentSequence.IsCompleting;
    }

    protected override ISequence CreateNewSequenceCore(string sequenceId, ConsumerPipelineContext context) =>
        new GatedBatchSequence(sequenceId + Guid.NewGuid().ToString("N"), context, _gate);

    protected override async ValueTask<ISequence?> GetExistingSequenceAsync(ConsumerPipelineContext context, string sequenceId) =>
        await context.SequenceStore.GetAsync<ISequence>(sequenceId, true) ??
        await CreateNewSequenceAsync(sequenceId, context);
}
