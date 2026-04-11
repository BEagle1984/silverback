// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Sequences;

namespace Silverback.Tests.Concurrency;

// A minimal concrete SequenceBase for Coyote tests. We subclass RawSequence rather than
// BatchSequence/ChunkSequence to avoid pulling in the timeout timer (a fire-and-forget
// Task.Run polling loop) and the chunk-ordering state machine, neither of which is
// relevant to the completion/abort synchronization we want Coyote to explore.
internal sealed class TestSequence : RawSequence
{
    public TestSequence(string sequenceId, ConsumerPipelineContext context)
        : base(sequenceId, context, enforceTimeout: false)
    {
    }
}
