// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Reflection;
using System.Threading.Tasks;
using Shouldly;
using Silverback.Messaging.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Concurrency;

// Audit findings C2 and H4 — both rooted in MessageStreamProvider<T>.
//
// C2: Abort() and CompleteAsync() are not safe to call concurrently. The audit
//     predicted a silent no-op via the early-return guard
//         if (_isAborting || _isCompleting) return;
//     but there is also a second, orthogonal failure mode: once CompleteAsync
//     has finished and `_completed = true`, a subsequent Abort() reaches the
//     line `if (_completed) throw new InvalidOperationException(...)` and
//     throws. Both manifestations violate the invariant "Abort() called
//     concurrently with an in-flight CompleteAsync() must either wait for
//     the complete then observe the completed state, or actually abort the
//     streams; it must never silently no-op and must never throw." Coyote
//     catches whichever manifestation its scheduler hits first — in this
//     repo it lands on the throw variant within a few iterations.
//
// H4: _lazyStreams is added to under `lock (_lazyStreams)` but read without
//     the lock in StreamsCount, Abort, CompleteAsync, and PushToCompatibleStreams.
//     Racing CreateStream() against PushAsync() can therefore either throw
//     from the enumerator version check or silently drop the newly added
//     stream from a push.
//
// Both tests currently fail on master. Fix directions tracked in the audit;
// broadly: replace the re-entry guard in Abort/CompleteAsync with a proper
// wait-then-observe pattern, and read _lazyStreams only under the lock (or
// switch to a ConcurrentBag/ImmutableArray<T>).
public class MessageStreamProviderCoyoteTests
{
    private readonly ITestOutputHelper _output;

    public MessageStreamProviderCoyoteTests(ITestOutputHelper output)
    {
        _output = output;
    }

    // C2
    [Fact]
    public void Abort_RacingCompleteAsync_ShouldNotReturnAsSilentNoOp()
    {
        // Race CompleteAsync() against Abort() on an otherwise-idle MessageStreamProvider.
        // The provider has no lazy streams, so CompleteAsync's ParallelForEachAsync body is
        // essentially empty, but the method still runs through its guard:
        //
        //   1. early-return if (_isCompleting || _isAborting) return;
        //   2. await _completeSemaphore.WaitAsync(cancellationToken);
        //   3. try { ... _isCompleting = true; ... _completed = true; }
        //      finally { _isCompleting = false; _completeSemaphore.Release(); }
        //
        // The symmetric Abort() has the same guard. If Coyote interleaves Abort() between
        // steps 3's `_isCompleting = true` and the finally's reset, Abort falls into the
        // early-return and silently no-ops: neither `_aborted` nor `_completed` is true
        // at the moment Abort returns, even though the caller explicitly asked to abort.
        //
        // We snapshot the private `_aborted` / `_completed` fields via reflection immediately
        // after Abort returns and assert that at least one of them is true. Reflection is
        // used because the fields are `private`; `InternalsVisibleTo` only opens `internal`.
        //
        // Currently fails on master. Fix direction (audit C2): replace the re-entry guard
        // with a proper wait-then-observe pattern so a losing racer either waits for the
        // in-progress operation or actually performs its work.
        CoyoteTestRunner.Run(
            async () =>
            {
                MessageStreamProvider<string> provider = new();

                Task producer = Task.Run(
                    async () => await provider.CompleteAsync().ConfigureAwait(false));

                bool abortObservedTerminalState = true;
                Task aborter = Task.Run(
                    () =>
                    {
                        provider.Abort();
                        abortObservedTerminalState = ReadTerminalFlags(provider);
                    });

                await Task.WhenAll(producer, aborter).ConfigureAwait(false);

                abortObservedTerminalState.ShouldBeTrue(
                    "MessageStreamProvider.Abort() returned while _isCompleting was set, " +
                    "leaving the provider in neither _aborted nor _completed state. " +
                    "Caller asked to abort but Abort silently no-opped on cross-thread reentry. " +
                    "See audit finding C2.");
            },
            _output);
    }

    private static bool ReadTerminalFlags(MessageStreamProvider<string> provider)
    {
        System.Type type = typeof(MessageStreamProvider<string>);
        bool aborted = (bool)type
            .GetField("_aborted", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(provider)!;
        bool completed = (bool)type
            .GetField("_completed", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(provider)!;
        return aborted || completed;
    }

    // H4 — deferred for this batch. The enumerator path inside MessageStreamEnumerable<T>
    // interacts with Coyote's rewritten AsyncTaskMethodBuilder in a way that crashes the
    // test host (double SetResult). Needs investigation. Left as [Fact(Skip=...)] so it
    // shows up in the report; un-skip once the rewriter interaction is understood or the
    // test is restructured to avoid await foreach.
    [Fact(Skip = "Coyote rewriter interaction with MessageStreamEnumerable async enumerator; see audit H4 follow-up")]
    public void CreateStream_RacingPushAsync_ShouldNeitherThrowNorDropTheNewStream()
    {
        CoyoteTestRunner.Run(
            async () =>
            {
                MessageStreamProvider<string> provider = new();

                // Pre-create one stream so _lazyStreams starts non-empty and PushToCompatibleStreams
                // actually iterates something.
                IMessageStreamEnumerable<string> first = provider.CreateStream<string>();
                Task drainFirst = Task.Run(
                    async () =>
                    {
                        try
                        {
                            await foreach (string msg in first.ConfigureAwait(false))
                            {
                                _ = msg;
                                await Task.Yield();
                            }
                        }
                        catch
                        {
                            // swallow abort/complete exceptions
                        }
                    });

                // T1: keep pushing while T2 adds a second stream.
                Task pushing = Task.Run(
                    async () => await provider.PushAsync("msg").ConfigureAwait(false));

                IMessageStreamEnumerable<string>? second = null;
                Task adding = Task.Run(
                    () =>
                    {
                        second = provider.CreateStream<string>();
                    });

                await Task.WhenAll(pushing, adding).ConfigureAwait(false);

                // Cleanly tear down so drainFirst completes.
                await provider.CompleteAsync().ConfigureAwait(false);
                await drainFirst.ConfigureAwait(false);

                second.ShouldNotBeNull();
            },
            _output);
    }
}
