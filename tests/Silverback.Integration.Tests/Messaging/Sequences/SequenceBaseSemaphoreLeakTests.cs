// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Shouldly;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Sequences.Chunking;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Sequences;

// Non-Coyote reproducer for a semaphore leak in SequenceBase<TEnvelope>.AbortIfIncompleteAsync.
// When the sequence is already aborted (or completed), AbortIfIncompleteAsync acquires
// _completeSemaphoreSlim and then early-returns on the !IsPending check without releasing
// it. The leak only bites on the next synchronous wait on that semaphore, which happens
// inside Dispose().
public class SequenceBaseSemaphoreLeakTests
{
    [Fact]
    public async Task AbortIfIncompleteAsync_AfterAbort_ShouldNotLeakCompleteSemaphore()
    {
        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
        ChunkSequence sequence = new("leak-test", 10, context);

        await sequence.AbortAsync(SequenceAbortReason.IncompleteSequence);
        sequence.IsAborted.ShouldBeTrue();

        // This is the call that leaks: it acquires _completeSemaphoreSlim, sees !IsPending,
        // and returns without releasing.
        await sequence.AbortIfIncompleteAsync();

        // Dispose() performs a synchronous Wait() on _completeSemaphoreSlim. If the semaphore
        // is leaked it will block here forever; we wrap the call in Task.Run with a timeout so
        // the test fails cleanly rather than hanging.
        Task disposeTask = Task.Run(sequence.Dispose);
        Task finished = await Task.WhenAny(disposeTask, Task.Delay(TimeSpan.FromSeconds(5)));

        finished.ShouldBe(disposeTask, "Dispose() blocked — AbortIfIncompleteAsync leaked _completeSemaphoreSlim.");
    }
}
