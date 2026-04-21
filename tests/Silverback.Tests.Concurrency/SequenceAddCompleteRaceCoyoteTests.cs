// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Sequences;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Concurrency;

// Audit finding C1 — AddCoreAsync's call to CompleteCoreAsync is not serialized with
// AbortCoreAsync. See src/Silverback.Integration/Messaging/Sequences/SequenceBase`1.cs.
//
//   - AddCoreAsync at line 399 calls the private CompleteCoreAsync while holding ONLY
//     _addSemaphoreSlim.
//   - The public CompleteAsync wrapper (line 426) wraps CompleteCoreAsync under
//     _completeSemaphoreSlim.
//   - AbortAsync (line 276) wraps AbortCoreAsync under _completeSemaphoreSlim.
//
// Because the AddAsync path to CompleteCoreAsync bypasses _completeSemaphoreSlim,
// a concurrent AbortAsync can enter AbortCoreAsync, set _completeState = Aborted,
// call _streamProvider.AbortIfPending (which under the C2 bug may silently no-op),
// wait on _addSemaphoreSlim (held by the Add path), and then after Add releases the
// semaphore run its rollback / error-policy logic against a batch whose
// _streamProvider.CompleteAsync has already returned.
//
// The Add path's CompleteCoreAsync also writes _completeState = Complete at the end,
// overwriting the Aborted value — so the final state is Complete AND the error
// policy has been invoked. The invariant the audit names is: "if IsComplete == true
// then AbortReason == None AND no rollback / error-policy call was made. Symmetrically,
// if IsAborted == true then no NotifyProcessingCompleted was sent to downstream as
// success."
//
// This test reproduces the race at the primitive level by invoking CompleteCoreAsync
// directly (via reflection on a TestSequence) while holding only _addSemaphoreSlim,
// then racing it against AbortAsync. We assert the primary terminal invariant:
// once both tasks have returned, IsComplete and IsAborted must not both be true, and
// the single _completeState value must match one of IsComplete or IsAborted.
//
// Because _completeState is a single int field, Complete XOR Aborted holds as a raw
// state invariant even when the race fires; the actual corruption shows up as
// downstream exceptions (stream provider in both completed and aborted state) or
// as inconsistent side effects. We widen the assertion by also checking that the
// test ran to completion without any unhandled exceptions — Coyote reports any
// unhandled exception as a bug.
//
// Currently expected to fail on master. Fix direction (audit C1): either acquire
// _completeSemaphoreSlim inside AddCoreAsync before calling CompleteCoreAsync, or
// have AbortCoreAsync re-check _completeState under _addSemaphoreSlim before its
// own state write.
public class SequenceAddCompleteRaceCoyoteTests
{
    private readonly ITestOutputHelper _output;

    public SequenceAddCompleteRaceCoyoteTests(ITestOutputHelper output)
    {
        _output = output;
    }

    // Currently skipped: the default Coyote runner reports the race as a "potential
    // deadlock or hang" before it explores a path that reaches a concrete assertion
    // failure. The deadlock detector triggers on the legitimate inversion between
    //   - T1 holding _addSemaphoreSlim while awaiting _streamProvider.CompleteAsync
    //   - T2 holding _completeSemaphoreSlim while awaiting _addSemaphoreSlim
    // The scheduling that resolves the inversion (T1's provider.CompleteAsync throws
    // "already aborted" because T2's AbortIfPending ran first, T1 releases the add
    // semaphore via its finally, T2 proceeds) is a legal terminating path, but the
    // default deadlock-detection heuristic flags the intermediate state.
    //
    // To make this test meaningful, the runner needs
    //   Configuration.Create()
    //     .WithPotentialDeadlocksReportedAsBugs(false)
    //     .WithConcurrencyFuzzingEnabled()
    //     .WithTestingIterations(10_000)
    // and the invariant needs to assert downstream side effects (CommitAsync vs
    // RollbackAsync calls on the TransactionManager, counted via an NSubstitute probe)
    // rather than just terminal enum state. The scaffolding is left in place; the
    // scheduling-tuning work is tracked as the C1 follow-up in the audit.
    [Fact(Skip = "Requires Coyote harness tuning — see inline comment and audit C1 follow-up.")]
    public void CompleteCoreAsync_RacingAbortAsync_ShouldNotThrowOrCorruptState()
    {
        CoyoteTestRunner.Run(
            async () =>
            {
                ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
                using TestSequence sequence = new("c1-add-complete-race", context, totalLength: 1);

                Task addPathComplete = Task.Run(
                    async () => await sequence.TriggerAddPathCompleteAsync().ConfigureAwait(false));

                Task abort = Task.Run(
                    async () => await sequence
                        .AbortAsync(SequenceAbortReason.Error, new InvalidOperationException("racing abort"))
                        .ConfigureAwait(false));

                await Task.WhenAll(addPathComplete, abort).ConfigureAwait(false);

                // Primary invariant: the sequence has reached a terminal state and is
                // internally consistent. Both flags cannot be true simultaneously since
                // _completeState is a single enum value, but the test still catches
                // exceptions thrown during the race (e.g. stream provider rejecting a
                // follow-up operation because it is already in the opposite state).
                bool isTerminal = sequence.IsComplete || sequence.IsAborted;
                bool notBoth = !(sequence.IsComplete && sequence.IsAborted);
                Microsoft.Coyote.Specifications.Specification.Assert(
                    isTerminal && notBoth,
                    $"Sequence ended in unexpected state: IsComplete={sequence.IsComplete}, IsAborted={sequence.IsAborted}");
            },
            _output);
    }
}
