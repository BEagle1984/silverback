// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Shouldly;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Sequences;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Concurrency;

// Systematic concurrency tests for SequenceBase<TEnvelope> completion / abort synchronization,
// executed under the Microsoft Coyote scheduler.
//
// Each [Fact] wraps a self-contained async test body in Coyote's TestingEngine, which explores
// different interleavings of the Task-based plumbing. A run is considered successful if no bug
// is found across all scheduled iterations; otherwise we fail the xunit test and emit Coyote's
// replay report (which includes a deterministic schedule to reproduce the bug).
//
// The assemblies under test (Silverback.Core, Silverback.Integration, and this test DLL) are
// binary-rewritten by `coyote rewrite` as a post-build step in the .csproj so that async
// state machines, SemaphoreSlim, Task.Run, etc. are intercepted by Coyote.
public class SequenceAbortCoyoteTests
{
    private readonly ITestOutputHelper _output;

    public SequenceAbortCoyoteTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ConcurrentAbort_ShouldReachAbortedStateExactlyOnce()
    {
        CoyoteTestRunner.Run(
            async () =>
            {
                ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
                using TestSequence sequence = new("seq-concurrent-abort", context);

                Task abort1 = sequence.AbortAsync(SequenceAbortReason.IncompleteSequence);
                Task abort2 = sequence.AbortAsync(SequenceAbortReason.IncompleteSequence);
                Task abort3 = sequence.AbortAsync(SequenceAbortReason.IncompleteSequence);

                await Task.WhenAll(abort1, abort2, abort3).ConfigureAwait(false);

                // Invariants: once any abort has returned, the sequence must be consistently
                // aborted; it must never be pending, and the abort reason must be the one we
                // requested (no spurious upgrade from the idempotent path).
                sequence.IsAborted.ShouldBeTrue();
                sequence.IsPending.ShouldBeFalse();
                sequence.AbortReason.ShouldBe(SequenceAbortReason.IncompleteSequence);
            },
            _output);
    }

    [Fact]
    public void AbortThenAbortIfIncomplete_ShouldBothReturnAndRemainAborted()
    {
        CoyoteTestRunner.Run(
            async () =>
            {
                ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
                using TestSequence sequence = new("seq-abort-plus-abort-if-incomplete", context);

                Task abort = sequence.AbortAsync(SequenceAbortReason.Error, new InvalidOperationException("boom"));
                Task abortIfIncomplete = sequence.AbortIfIncompleteAsync();

                await Task.WhenAll(abort, abortIfIncomplete).ConfigureAwait(false);

                // The first abort carries the higher-priority reason (Error) and must win.
                // AbortIfIncompleteAsync racing against it must not downgrade the reason nor
                // leave the sequence in a partially-aborted state.
                sequence.IsAborted.ShouldBeTrue();
                sequence.IsPending.ShouldBeFalse();
                sequence.AbortReason.ShouldBe(SequenceAbortReason.Error);
            },
            _output);
    }

    [Fact]
    public void ConcurrentAbortMix_ShouldConvergeToSingleAbortedStateWithoutSemaphoreLeak()
    {
        // Three concurrent tasks: two AbortAsync(IncompleteSequence) and one AbortIfIncompleteAsync.
        // This mixes the two code paths that both take _completeSemaphoreSlim, so any
        // release-path asymmetry (such as the bug we found and fixed in AbortIfIncompleteAsync)
        // will manifest as either a deadlock or a stuck semaphore that Dispose() then hits.
        CoyoteTestRunner.Run(
            async () =>
            {
                ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
                using TestSequence sequence = new("seq-abort-mix", context);

                Task abort1 = sequence.AbortAsync(SequenceAbortReason.IncompleteSequence);
                Task abort2 = sequence.AbortAsync(SequenceAbortReason.IncompleteSequence);
                Task abortIfIncomplete = sequence.AbortIfIncompleteAsync();

                await Task.WhenAll(abort1, abort2, abortIfIncomplete).ConfigureAwait(false);

                sequence.IsAborted.ShouldBeTrue();
                sequence.IsPending.ShouldBeFalse();
                sequence.AbortReason.ShouldBe(SequenceAbortReason.IncompleteSequence);
            },
            _output);
    }

    [Fact]
    public void ConcurrentErrorAborts_ShouldSetAbortExceptionExactlyOnce()
    {
        // Two concurrent AbortAsync(Error, ...) calls. The first to acquire _completeSemaphoreSlim
        // should set AbortException; the second should observe IsAborted, wait on
        // _abortingTaskCompletionSource, and return. AbortException must be exactly one of the two
        // exceptions we passed in — no null, no partial state.
        CoyoteTestRunner.Run(
            async () =>
            {
                ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
                using TestSequence sequence = new("seq-concurrent-error", context);

                InvalidOperationException ex1 = new("first");
                InvalidOperationException ex2 = new("second");

                Task abort1 = sequence.AbortAsync(SequenceAbortReason.Error, ex1);
                Task abort2 = sequence.AbortAsync(SequenceAbortReason.Error, ex2);

                await Task.WhenAll(abort1, abort2).ConfigureAwait(false);

                sequence.IsAborted.ShouldBeTrue();
                sequence.AbortReason.ShouldBe(SequenceAbortReason.Error);
                sequence.AbortException.ShouldNotBeNull();
                Exception abortException = sequence.AbortException;
                (abortException == ex1 || abortException == ex2).ShouldBeTrue(
                    "AbortException must be one of the exceptions that was passed in, not a wrapper or null.");
            },
            _output);
    }
}
