// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Sequences.Batch;

namespace Silverback.Tests.Integration.E2E.TestTypes;

// A batch whose elapsed max wait time can be held (see BatchTimeoutGate)
public sealed class GatedBatchSequence : BatchSequence
{
    private readonly BatchTimeoutGate _gate;

    public GatedBatchSequence(string sequenceId, ConsumerPipelineContext context, BatchTimeoutGate gate)
        : base(sequenceId, context)
    {
        _gate = gate;
    }

    protected override async Task OnTimeoutElapsedAsync()
    {
        await _gate.HoldFirstTimeoutAsync();
        await base.OnTimeoutElapsedAsync();
    }
}
