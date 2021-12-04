// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

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

    public FakeSequence(string sequenceId, bool isComplete, bool isAborted, ISequenceStore store)
        : base(sequenceId, ConsumerPipelineContextHelper.CreateSubstitute(sequenceStore: store))
    {
        if (isComplete)
            CompleteAsync().Wait();

        if (isAborted)
            AbortAsync(SequenceAbortReason.EnumerationAborted).Wait();
    }
}
