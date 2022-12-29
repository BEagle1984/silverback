// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;

namespace Silverback.Tests.Types;

public sealed class FakeSequence : SequenceBase<IInboundEnvelope>
{
    public FakeSequence()
        : base("fake1", ConsumerPipelineContextHelper.CreateSubstitute())
    {
        Length = 3;
    }

    [SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Reviewed")]
    public FakeSequence(string sequenceId, bool isComplete, bool isAborted, ISequenceStore store)
        : base(sequenceId, ConsumerPipelineContextHelper.CreateSubstitute(sequenceStore: store))
    {
        if (isComplete)
            CompleteAsync().AsTask().Wait();

        if (isAborted)
            AbortAsync(SequenceAbortReason.EnumerationAborted).AsTask().Wait();
    }
}
