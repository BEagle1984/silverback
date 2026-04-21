// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Kafka;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Concurrency;

// Kafka-specific concurrency tests targeting audit findings H5, H6, H7, and M2.
//
// H5, H6, and H7 are pattern-level tests: they extract the production code's
// synchronization pattern into standalone code that Coyote can schedule, rather
// than constructing the full KafkaConsumer / ConsumeLoopHandler with all their
// dependencies. This means the tests document and catch the BUG PATTERN but do
// not regression-protect the exact production class. When the pattern is fixed
// in production, these tests should be updated to mirror the new pattern.
//
// M2 tests the real OffsetsTracker class (public, zero dependencies).
public class KafkaConcurrencyPatternCoyoteTests
{
    private readonly ITestOutputHelper _output;

    public KafkaConcurrencyPatternCoyoteTests(ITestOutputHelper output)
    {
        _output = output;
    }

    // H5 — Kafka commit TOCTOU on revoked partitions.
    //
    // KafkaConsumer.CommitCoreAsync (lines 251-279) filters offsets by IsNotRevoked
    // using a LINQ Where, then double-checks IsNotRevoked inside the loop body.
    // Both checks read _revokedPartitions (ConcurrentDictionary) which is concurrently
    // mutated by OnPartitionsRevoked from the rebalance callback thread.
    //
    // The TOCTOU window: a partition passes both checks, then gets revoked before
    // StoreOffset runs. The stored offset is for a partition the consumer no longer
    // owns, which violates exactly-once semantics.
    //
    // The test models the pattern: an enumerable filtered by "not revoked", a loop
    // body that re-checks "not revoked" then stores, and a concurrent revoke task
    // that adds to the revoked set. If Coyote interleaves the revoke between the
    // inner check and the store, the invariant fires.
    [Fact]
    public void CommitCoreAsync_RevokeRacingCommit_ShouldNotStoreOffsetForRevokedPartition()
    {
        CoyoteTestRunner.Run(
            async () =>
            {
                ConcurrentDictionary<TopicPartition, byte> revokedPartitions = new();
                TopicPartition tp0 = new("topic", new Partition(0));
                TopicPartition tp1 = new("topic", new Partition(1));

                List<TopicPartition> allPartitions = [tp0, tp1];
                ConcurrentBag<TopicPartition> storedOffsets = new();

                // T1: commit path — mirrors CommitCoreAsync lines 258-275
                Task commit = Task.Run(
                    async () =>
                    {
                        IEnumerable<TopicPartition> filtered = allPartitions
                            .Where(tp => !revokedPartitions.ContainsKey(tp));

                        foreach (TopicPartition tp in filtered)
                        {
                            await Task.Yield(); // scheduling point for Coyote

                            if (!revokedPartitions.ContainsKey(tp)) // inner check (still TOCTOU)
                            {
                                await Task.Yield(); // one more point between check and store
                                storedOffsets.Add(tp); // mirrors StoreOffset(...)
                            }
                        }
                    });

                // T2: revoke path — mirrors OnPartitionsRevoked adding to _revokedPartitions
                Task revoke = Task.Run(
                    async () =>
                    {
                        await Task.Yield();
                        revokedPartitions.TryAdd(tp1, 0);
                    });

                await Task.WhenAll(commit, revoke).ConfigureAwait(false);

                // Invariant: no stored offset should be for a partition that is now revoked.
                foreach (TopicPartition stored in storedOffsets)
                {
                    revokedPartitions.ContainsKey(stored).ShouldBeFalse(
                        $"Stored offset for revoked partition {stored}. " +
                        "The TOCTOU window between IsNotRevoked check and StoreOffset " +
                        "allowed a commit for a partition the consumer no longer owns. " +
                        "See audit H5.");
                }
            },
            _output);
    }

    // H6 — OnPartitionsRevoked commits after fire-and-forget consume-loop stop.
    //
    // KafkaConsumer.OnPartitionsRevoked (lines 152-174):
    //   _consumeLoopHandler.StopAsync().FireAndForget();  // does NOT await
    //   Task.WhenAll(...StopReadingAsync...).SafeWait();
    //   if (!Configuration.EnableAutoCommit)
    //       Client.Commit();
    //
    // The consume loop keeps running after StopAsync is fired-and-forgotten. It may
    // consume and write messages into channels between the moment StopAsync is triggered
    // and the moment Client.Commit() runs. Those messages are included in the committed
    // offsets but were never fully processed, violating exactly-once semantics.
    //
    // The test models the pattern: a "stop" task that takes time to complete, a "commit"
    // that runs before the stop finishes, and a "consume" task that produces work items
    // in between. Assert: no work item is produced after commit.
    [Fact]
    public void OnPartitionsRevoked_CommitBeforeStopCompletes_ShouldNotCommitUnprocessedMessages()
    {
        CoyoteTestRunner.Run(
            async () =>
            {
                ConcurrentBag<string> consumed = new();
                ConcurrentBag<string> committed = new();
                SemaphoreSlim stopGate = new(0, 1);
                bool stopRequested = false;
                bool commitDone = false;

                // Consume loop: keeps producing until stopped
                Task consumeLoop = Task.Run(
                    async () =>
                    {
                        int i = 0;
                        while (!stopRequested)
                        {
                            consumed.Add($"msg-{i++}");
                            await Task.Yield();
                        }

                        // The real consume loop doesn't stop instantly; it may
                        // produce one more message after the flag is set.
                        consumed.Add($"msg-{i}-after-stop");
                    });

                // Revoke handler pattern
                Task revokeHandler = Task.Run(
                    async () =>
                    {
                        // Fire-and-forget stop
                        Task stopTask = Task.Run(
                            async () =>
                            {
                                await stopGate.WaitAsync().ConfigureAwait(false);
                                stopRequested = true;
                            });
                        // Do NOT await stopTask — fire and forget

                        await Task.Yield(); // simulates StopReadingAsync().SafeWait()

                        // Commit: snapshot whatever consumed is the "committed position"
                        foreach (string msg in consumed)
                        {
                            committed.Add(msg);
                        }

                        commitDone = true;

                        // Now release the stop gate so the consume loop eventually ends
                        stopGate.Release();
                        await stopTask.ConfigureAwait(false);
                    });

                await Task.WhenAll(consumeLoop, revokeHandler).ConfigureAwait(false);

                // Invariant: everything committed should have been consumed BEFORE commit.
                // Messages consumed AFTER commit are lost (committed offsets advanced past them
                // but they were never processed).
                // In this simplified model, the committed set was snapshotted from consumed,
                // so the committed messages are a subset of consumed. But the consume loop may
                // have added MORE messages after the snapshot, and those messages are "lost"
                // because the committed offsets would include the produce-loop's latest position.
                //
                // The test checks: did the consume loop produce any message after commitDone
                // was set? If so, the fire-and-forget pattern is unsound.
                int consumedAfterCommit = consumed.Count - committed.Count;
                consumedAfterCommit.ShouldBe(
                    0,
                    $"Consumed {consumedAfterCommit} message(s) after commit ran. " +
                    "OnPartitionsRevoked fires StopAsync but does not await it, " +
                    "then calls Client.Commit(). Messages consumed between the " +
                    "fire-and-forget and the commit are included in the committed " +
                    "offset position but were never fully processed. " +
                    "See audit H6.");
            },
            _output);
    }

    // H7 — ConsumeLoopHandler.Start/Stop racy on non-volatile IsConsuming.
    //
    // ConsumeLoopHandler.Start() (lines 54-81):
    //   if (IsConsuming) return;    // non-volatile read
    //   IsConsuming = true;
    //   Task.Factory.StartNew(...ConsumeAsync...).FireAndForget();
    //
    // Two concurrent Start() callers can both read IsConsuming=false and both
    // start a consume loop. Two loops consuming from the same Confluent consumer
    // causes undefined behavior (double-poll, offset confusion).
    [Fact]
    public void ConsumeLoopHandlerStart_ConcurrentCalls_ShouldStartExactlyOneLoop()
    {
        CoyoteTestRunner.Run(
            async () =>
            {
                bool isConsuming = false;
                int loopsStarted = 0;

                Task Start()
                {
                    // Mirrors ConsumeLoopHandler.Start() lines 58-81
                    if (isConsuming)
                        return Task.CompletedTask;

                    isConsuming = true;

                    Interlocked.Increment(ref loopsStarted);

                    // Simulate Task.Factory.StartNew(ConsumeAsync).FireAndForget()
                    Task.Run(
                        async () =>
                        {
                            await Task.Yield(); // simulates the consume loop body
                        }).FireAndForget();

                    return Task.CompletedTask;
                }

                Task t1 = Task.Run(Start);
                Task t2 = Task.Run(Start);

                await Task.WhenAll(t1, t2).ConfigureAwait(false);
                await Task.Yield(); // let fire-and-forget loops settle

                loopsStarted.ShouldBe(
                    1,
                    $"Started {loopsStarted} consume loops instead of 1. " +
                    "Two concurrent Start() calls both read IsConsuming=false " +
                    "(non-volatile bool) and both started a loop. " +
                    "See audit H7.");
            },
            _output);
    }

    // H7 aggressive — same pattern as above but with Task.Yield() inserted
    // between the check and the set to give Coyote an explicit scheduling point.
    // The default 100-iteration run passes because the check-then-act is
    // synchronous with no yield between the two statements. The aggressive
    // variant proves the pattern is unsafe by widening the race window to a
    // point Coyote can schedule.
    [Fact]
    public void ConsumeLoopHandlerStart_ConcurrentCalls_Aggressive_ShouldStartExactlyOneLoop()
    {
        CoyoteTestRunner.RunAggressive(
            async () =>
            {
                bool isConsuming = false;
                int loopsStarted = 0;

                async Task Start()
                {
                    if (isConsuming)
                        return;

                    await Task.Yield(); // scheduling point at the race window

                    isConsuming = true;
                    Interlocked.Increment(ref loopsStarted);

                    Task.Run(
                        async () =>
                        {
                            await Task.Yield();
                        }).FireAndForget();
                }

                Task t1 = Task.Run(Start);
                Task t2 = Task.Run(Start);

                await Task.WhenAll(t1, t2).ConfigureAwait(false);
                await Task.Yield();

                loopsStarted.ShouldBe(
                    1,
                    $"Started {loopsStarted} consume loops instead of 1. " +
                    "Two concurrent Start() calls both read IsConsuming=false " +
                    "and both started a loop. See audit H7.");
            },
            _output,
            iterations: 1_000);
    }

    // M2 — OffsetsTracker.TrackOffset performs two non-atomic AddOrUpdates.
    //
    // OffsetsTracker.TrackOffset (lines 27-42) updates _commitOffsets and
    // _rollbackOffsets in two separate AddOrUpdate calls. A concurrent reader
    // calling GetCommitOffsets() + GetRollbackOffsets() can observe a state
    // where one dict has been updated but the other hasn't yet.
    //
    // The test writes offsets from one task while reading from another and
    // checks that every partition visible in rollback offsets is also visible
    // in commit offsets (they should always appear as a pair since TrackOffset
    // writes both).
    [Fact]
    public void OffsetsTracker_ConcurrentTrackAndRead_ShouldNotObservePartialState()
    {
        CoyoteTestRunner.Run(
            async () =>
            {
                OffsetsTracker tracker = new();
                TopicPartition tp = new("topic", new Partition(0));
                bool observedPartialState = false;

                // T1: repeatedly track offsets
                Task writer = Task.Run(
                    async () =>
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            tracker.TrackOffset(new KafkaOffset(tp, new Offset(i)));
                            await Task.Yield();
                        }
                    });

                // T2: repeatedly read both dicts and check consistency
                Task reader = Task.Run(
                    async () =>
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            var commits = tracker.GetCommitOffsets();
                            await Task.Yield(); // scheduling point between the two reads
                            var rollbacks = tracker.GetRollbackOffSets();

                            // If rollbacks has a key that commits doesn't, the reader
                            // observed a partial state (second AddOrUpdate ran before first).
                            // Vice versa is also partial but less likely to cause issues.
                            foreach (KafkaOffset rb in rollbacks)
                            {
                                if (!commits.Any(c => c.TopicPartition == rb.TopicPartition))
                                {
                                    observedPartialState = true;
                                }
                            }

                            await Task.Yield();
                        }
                    });

                await Task.WhenAll(writer, reader).ConfigureAwait(false);

                observedPartialState.ShouldBeFalse(
                    "Reader observed a partition in rollback offsets that was absent from " +
                    "commit offsets. OffsetsTracker.TrackOffset performs two sequential " +
                    "AddOrUpdate calls that are not atomic as a pair. " +
                    "See audit M2.");
            },
            _output);
    }
}

// Minimal extension to mirror Silverback.Util.TaskExtensions.FireAndForget,
// which is internal. The Coyote rewriter intercepts Task.Run and scheduling;
// the extension itself just suppresses the compiler warning.
file static class FireAndForgetExtensions
{
    public static void FireAndForget(this Task task) => _ = task;
}
