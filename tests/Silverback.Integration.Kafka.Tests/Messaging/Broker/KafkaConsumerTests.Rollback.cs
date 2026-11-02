// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Shouldly;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker;

public partial class KafkaConsumerTests
{
    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, false, false)]
    [InlineData(true, true, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, true)]
    [InlineData(false, false, true)]
    [InlineData(true, true, true)]
    [InlineData(false, true, true)]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The poll and rollback complete before the harness is disposed")]
    public async Task Rollback_ShouldDrainPendingPollBeforeSeekingAndReplaying(bool independent, bool autoCommit, bool beforeConsumeReturns)
    {
        await using PollHarness harness = new(independent, autoCommit: autoCommit);
        await harness.StartAsync(0);

        ProcessingGate active = harness.Block(0, 0);
        active.SkipCommit = true;
        await harness.DeliverAsync((0, 0), (0, 1), (0, 2));
        await active.Started.Task.WaitAsync(Timeout);

        TaskCompletionSource<bool> recordPolled = NewSignal();
        TaskCompletionSource<bool> releasePoll = NewSignal();

        if (!beforeConsumeReturns)
            harness.AfterConsume = BlockPoll;

        Task poll = harness.PollAsync(() =>
        {
            ConsumeResult<byte[]?, byte[]?> record = harness.Record(0, 5);

            if (beforeConsumeReturns)
                BlockPoll();

            return record;
        });

        try
        {
            await recordPolled.Task.WaitAsync(Timeout);
            await harness.Consumer.RollbackAsync(active.Context!.Envelope.BrokerMessageIdentifier);
            await active.ReaderStopped.Task.WaitAsync(Timeout);

            // Seeking while a fetched record is still outstanding lets it enter the replacement before replay
            harness.Seeks.ShouldBeEmpty();
            harness.Resumes.ShouldBeEmpty();
        }
        finally
        {
            harness.AfterConsume = null;
            active.Release.TrySetResult(true);
            releasePoll.TrySetResult(true);
            await poll.WaitAsync(Timeout);
        }

        await harness.RollbackRestartObserved.Task.WaitAsync(Timeout);
        await harness.DeliverAsync([.. Enumerable.Range(0, 6).Select(offset => (0, (long)offset))]);
        await PollHarness.WaitUntilAsync(() => harness.StoredOffset(0) == 6);
        harness.Client.Commit();

        _output.WriteLine($"Completed offsets after rollback: {string.Join(", ", harness.Completed.Select(delivery => delivery.Offset))}");

        harness.Completed.Select(delivery => delivery.Offset).ShouldBe([0L, 1L, 2L, 3L, 4L, 5L]);
        harness.CommittedOffset(0).ShouldBe(6);
        harness.UnsafeStores.ShouldBeEmpty();
        harness.UnsafeCommits.ShouldBeEmpty();
        harness.Errors.ShouldBeEmpty();

        return;

        void BlockPoll()
        {
            recordPolled.TrySetResult(true);
            releasePoll.Task.WaitAsync(Timeout).GetAwaiter().GetResult();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The pending poll is released before the harness is disposed")]
    public async Task Rollback_ShouldReplayFirstPolledRecord_WhenSharedPartitionPositionWasUnknown(bool autoCommit)
    {
        await using PollHarness harness = new(independent: false, autoCommit: autoCommit);
        await harness.StartAsync();
        await harness.PollAsync(() =>
        {
            harness.AssignOffsets(
            [
                new TopicPartitionOffset("topic", 0, Offset.Unset),
                new TopicPartitionOffset("topic", 1, Offset.Unset)
            ]);

            return null;
        });

        ProcessingGate active = harness.Block(0, 0);
        active.SkipCommit = true;
        await harness.DeliverAsync((0, 0), (0, 1));
        await active.Started.Task.WaitAsync(Timeout);

        TaskCompletionSource<bool> recordPolled = NewSignal();
        TaskCompletionSource<bool> releasePoll = NewSignal();
        Task poll = harness.PollAsync(() =>
        {
            recordPolled.TrySetResult(true);
            releasePoll.Task.WaitAsync(Timeout).GetAwaiter().GetResult();

            return harness.Record(1, 0);
        });

        try
        {
            await recordPolled.Task.WaitAsync(Timeout);
            await harness.Consumer.RollbackAsync(active.Context!.Envelope.BrokerMessageIdentifier);
            await active.ReaderStopped.Task.WaitAsync(Timeout);
        }
        finally
        {
            active.Release.TrySetResult(true);
            releasePoll.TrySetResult(true);
            await poll.WaitAsync(Timeout);
        }

        await PollHarness.WaitUntilAsync(() => harness.Resumes.Count == 2);

        harness.Seeks.ShouldContain(new TopicPartitionOffset("topic", 0, 0));
        harness.Seeks.ShouldContain(new TopicPartitionOffset("topic", 1, 0));

        await harness.DeliverAsync((0, 0), (0, 1), (1, 0), (1, 1));
        await PollHarness.WaitUntilAsync(() => harness.StoredOffset(0) == 2 && harness.StoredOffset(1) == 2);
        harness.Client.Commit();

        harness.Completed.Select(delivery => (delivery.Partition, delivery.Offset)).ShouldBe([(0, 0L), (0, 1L), (1, 0L), (1, 1L)]);
        harness.UnsafeStores.ShouldBeEmpty();
        harness.UnsafeCommits.ShouldBeEmpty();
        harness.Errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The pending poll is released before the harness is disposed")]
    public async Task Shutdown_ShouldCompleteAndDiscardRollback_WhenWaitingForPendingPoll(bool independent, bool reconnect)
    {
        await using PollHarness harness = new(independent);
        await harness.StartAsync(0);

        ProcessingGate active = harness.Block(0, 0);
        active.SkipCommit = true;
        await harness.DeliverAsync((0, 0), (0, 1));
        await active.Started.Task.WaitAsync(Timeout);

        TaskCompletionSource<bool> recordPolled = NewSignal();
        TaskCompletionSource<bool> releasePoll = NewSignal();
        Task poll = harness.PollAsync(() =>
        {
            recordPolled.TrySetResult(true);
            releasePoll.Task.WaitAsync(Timeout).GetAwaiter().GetResult();

            return harness.Record(0, 5);
        });

        try
        {
            await recordPolled.Task.WaitAsync(Timeout);
            await harness.Consumer.RollbackAsync(active.Context!.Envelope.BrokerMessageIdentifier);
            await active.ReaderStopped.Task.WaitAsync(Timeout);
            await harness.Consumer.StopAsync(false).AsTask().WaitAsync(Timeout);

            harness.PollingStopped.IsCompleted.ShouldBeFalse();
            harness.Seeks.ShouldBeEmpty();
            harness.Resumes.ShouldBeEmpty();
        }
        finally
        {
            active.Release.TrySetResult(true);
            releasePoll.TrySetResult(true);
            await poll.WaitAsync(Timeout);
        }

        await harness.Consumer.StopAsync().AsTask().WaitAsync(Timeout);
        await harness.RollbackRestartObserved.Task.WaitAsync(Timeout);

        harness.GetChannel(0).ShouldBeNull();
        harness.Completed.ShouldBeEmpty();

        // The records polled and never committed are consumed again from the first one after the restart, which also resumes
        // the partition paused by the rollback, while the defeated rollback must neither seek nor resume
        TopicPartitionOffset[] expectedSeeks = reconnect ? [new TopicPartitionOffset("topic", 0, 0)] : [];
        TopicPartition[] expectedResumes = reconnect ? [new TopicPartition("topic", 0)] : [];

        harness.Paused.ShouldBe([new TopicPartition("topic", 0)]);

        if (reconnect)
        {
            await harness.StartAsync(0);

            harness.Seeks.ShouldBe(expectedSeeks);
            harness.Paused.ShouldBeEmpty();

            await harness.DeliverAsync([.. Enumerable.Range(0, 6).Select(offset => (0, (long)offset))]);
            await harness.WaitForCommitAsync(0, 6);

            harness.Completed.Select(delivery => delivery.Offset).ShouldBe([0L, 1L, 2L, 3L, 4L, 5L]);
        }

        harness.Seeks.ShouldBe(expectedSeeks);
        harness.Resumes.ShouldBe(expectedResumes);
        harness.UnsafeStores.ShouldBeEmpty();
        harness.UnsafeCommits.ShouldBeEmpty();
        harness.Errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The rollback is awaited before the harness is disposed")]
    public async Task Restart_ShouldResumePartitionPausedByRollback_WhenStopDefeatedRollbackRestart(bool independent)
    {
        await using PollHarness harness = new(independent);
        await harness.StartAsync(0, 1);

        ProcessingGate active = harness.Block(0, 0);
        active.SkipCommit = true;
        await harness.DeliverAsync((0, 0), (0, 1));
        await active.Started.Task.WaitAsync(Timeout);

        TaskCompletionSource<bool> entered = NewSignal();
        TaskCompletionSource<bool> release = NewSignal();
        int stoppingCount = 0;
        active.OnReaderStopped = () =>
        {
            if (Interlocked.Increment(ref stoppingCount) != 1)
                return;

            entered.TrySetResult(true);

            release.Task.Wait(Timeout).ShouldBeTrue();
        };

        Task rollback = Task.Run(async () => await harness.Consumer.RollbackAsync(active.Context!.Envelope.BrokerMessageIdentifier));

        try
        {
            await entered.Task.WaitAsync(Timeout);
            await harness.Consumer.StopAsync().AsTask().WaitAsync(Timeout);

            // A partition paused by the user must stay paused, even if the rollback paused it as well (when sharing one channel)
            harness.Consumer.Pause([new TopicPartition("topic", 1)]);

            release.TrySetResult(true);
            await rollback.WaitAsync(Timeout);
            await PollHarness.WaitUntilAsync(() => harness.Logs.Any(log => log.StartsWith("Skipping rollback restart", StringComparison.Ordinal)));

            harness.Resumes.ShouldBeEmpty();
            harness.Paused.ShouldBe([new TopicPartition("topic", 0), new TopicPartition("topic", 1)], true);
        }
        finally
        {
            release.TrySetResult(true);
            active.Release.TrySetResult(true);
            await rollback.WaitAsync(Timeout);
        }

        int mutationsBeforeRestart = harness.NativeMutations.Count;
        await harness.Consumer.StartAsync();
        await harness.PollAsync(() => null);

        // The partition paused by the defeated rollback is resumed once sought back to its first uncommitted record
        harness.Seeks.ShouldContain(new TopicPartitionOffset("topic", 0, 0));
        harness.NativeMutations.Skip(mutationsBeforeRestart).SkipWhile(mutation => mutation == "seek").ShouldBe(["resume"]);
        harness.Resumes.ShouldBe([new TopicPartition("topic", 0)]);
        harness.Paused.ShouldBe([new TopicPartition("topic", 1)]);

        await harness.DeliverAsync((0, 0), (0, 1));
        await harness.WaitForCommitAsync(0, 2);

        harness.Completed.Select(delivery => delivery.Offset).ShouldBe([0L, 1L]);
        harness.UnsafeStores.ShouldBeEmpty();
        harness.UnsafeCommits.ShouldBeEmpty();
        harness.Errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The rollback is awaited before the harness is disposed")]
    public async Task Rebalance_ShouldResumePartitionPausedByRollback_WhenReassignedAfterDefeatingRollbackRestart(
        bool independent,
        bool cooperative,
        bool assignBeforeRestart)
    {
        await using PollHarness harness = new(independent, cooperative);
        await harness.StartAsync(0, 1);

        ProcessingGate active = harness.Block(0, 0);
        active.SkipCommit = true;
        await harness.DeliverAsync((0, 0), (0, 1));
        await active.Started.Task.WaitAsync(Timeout);

        TaskCompletionSource<bool> entered = NewSignal();
        TaskCompletionSource<bool> release = NewSignal();
        int stoppingCount = 0;
        active.OnReaderStopped = () =>
        {
            if (Interlocked.Increment(ref stoppingCount) != 1)
                return;

            entered.TrySetResult(true);

            release.Task.Wait(Timeout).ShouldBeTrue();
        };

        // The eager assignor revokes and assigns every partition again, the cooperative one only the partition that moves
        int[] reassigned = cooperative ? [0] : [0, 1];
        Task rollback = Task.Run(async () => await harness.Consumer.RollbackAsync(active.Context!.Envelope.BrokerMessageIdentifier));

        try
        {
            await entered.Task.WaitAsync(Timeout);

            harness.Paused.ShouldContain(new TopicPartition("topic", 0));

            Task revoke = harness.PollAsync(() =>
            {
                harness.Revoke(reassigned);

                return null;
            });

            await PollHarness.WaitUntilAsync(() => harness.Logs.Count(log => log.StartsWith("Stopping processing loop", StringComparison.Ordinal)) >= 2);
            active.Release.TrySetResult(true);
            await revoke.WaitAsync(Timeout);

            if (assignBeforeRestart)
                await AssignAsync();

            release.TrySetResult(true);
            await rollback.WaitAsync(Timeout);
            await PollHarness.WaitUntilAsync(() => harness.Logs.Any(log => log.StartsWith("Skipping rollback restart", StringComparison.Ordinal)));

            if (!assignBeforeRestart)
            {
                // The native pause survives the revocation
                harness.Paused.ShouldContain(new TopicPartition("topic", 0));

                await AssignAsync();
            }
        }
        finally
        {
            release.TrySetResult(true);
            active.Release.TrySetResult(true);
            await rollback.WaitAsync(Timeout);
        }

        // The new assignment resumes the partition paused by the defeated rollback, which neither seeks nor resumes it
        harness.Paused.ShouldBeEmpty();
        harness.Resumes.Count(partition => partition.Partition.Value == 0).ShouldBe(1);
        harness.Seeks.ShouldNotContain(offset => offset.Partition.Value == 0);

        await harness.DeliverAsync((0, 0), (0, 1));
        await harness.WaitForCommitAsync(0, 2);

        // A restart doesn't resume it again
        int resumes = harness.Resumes.Count;
        await harness.Consumer.StopAsync().AsTask().WaitAsync(Timeout);
        await harness.Consumer.StartAsync();
        await harness.PollAsync(() => null);

        harness.Resumes.Count.ShouldBe(resumes);
        harness.Starts.ShouldBe([new Delivery(0, 0, 1), new Delivery(0, 0, 2), new Delivery(0, 1, 2)]);
        harness.Completed.ShouldBe([new Delivery(0, 0, 2), new Delivery(0, 1, 2)]);
        harness.UnsafeStores.ShouldBeEmpty();
        harness.UnsafeCommits.ShouldBeEmpty();
        harness.Errors.ShouldBeEmpty();

        return;

        Task AssignAsync() => harness.PollAsync(() =>
        {
            harness.Assign(reassigned);

            return null;
        });
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The rollbacks are awaited before the harness is disposed")]
    public async Task Rebalance_ShouldResumeOnlyAssignedPartitionsPausedByRollback_WhenAssignmentIsIncremental()
    {
        await using PollHarness harness = new(independent: true, cooperative: true);
        await harness.StartAsync(0, 1);

        ProcessingGate active0 = harness.Block(0, 0);
        active0.SkipCommit = true;
        ProcessingGate active1 = harness.Block(1, 0);
        active1.SkipCommit = true;
        await harness.DeliverAsync((0, 0), (1, 0));
        await active0.Started.Task.WaitAsync(Timeout);
        await active1.Started.Task.WaitAsync(Timeout);

        TaskCompletionSource<bool> entered0 = NewSignal();
        TaskCompletionSource<bool> release0 = NewSignal();
        HoldFirstReaderStop(active0, entered0, release0);

        TaskCompletionSource<bool> entered1 = NewSignal();
        TaskCompletionSource<bool> release1 = NewSignal();
        HoldFirstReaderStop(active1, entered1, release1);

        TopicPartition partition0 = new("topic", 0);
        TopicPartition partition1 = new("topic", 1);
        Task rollback0 = Task.CompletedTask;
        Task rollback1 = Task.Run(async () => await harness.Consumer.RollbackAsync(active1.Context!.Envelope.BrokerMessageIdentifier));
        int mutations;

        try
        {
            // Partition 1 is revoked while being rolled back and isn't assigned again: it stays paused by the rollback
            await entered1.Task.WaitAsync(Timeout);

            Task revoke = harness.PollAsync(() =>
            {
                harness.Revoke(1);

                return null;
            });

            await PollHarness.WaitUntilAsync(() => harness.Logs.Count(log => log.StartsWith("Stopping processing loop", StringComparison.Ordinal)) >= 2);
            active1.Release.TrySetResult(true);
            await revoke.WaitAsync(Timeout);

            release1.TrySetResult(true);
            await rollback1.WaitAsync(Timeout);
            await PollHarness.WaitUntilAsync(() => harness.Logs.Any(log => log.StartsWith("Skipping rollback restart", StringComparison.Ordinal)));

            // Partition 2 is assigned incrementally while the rollback restart of partition 0 is pending
            rollback0 = Task.Run(async () => await harness.Consumer.RollbackAsync(active0.Context!.Envelope.BrokerMessageIdentifier));
            await entered0.Task.WaitAsync(Timeout);

            await harness.PollAsync(() =>
            {
                harness.Assign(2);

                return null;
            });

            // Resumed now, partition 0 would be fetched from its position preceding the seek of the rollback restart
            harness.Paused.ShouldBe([partition0, partition1], true);
            harness.Resumes.ShouldBeEmpty();

            mutations = harness.NativeMutations.Count;
            active0.Release.TrySetResult(true);
            release0.TrySetResult(true);
            await rollback0.WaitAsync(Timeout);
            await PollHarness.WaitUntilAsync(() => harness.Resumes.Contains(partition0));
        }
        finally
        {
            release0.TrySetResult(true);
            release1.TrySetResult(true);
            active0.Release.TrySetResult(true);
            active1.Release.TrySetResult(true);
            await rollback0.WaitAsync(Timeout);
            await rollback1.WaitAsync(Timeout);
        }

        // The rollback restart seeks partition 0 before resuming it, while partition 1 stays paused until it's assigned again
        harness.NativeMutations.Skip(mutations).ShouldBe(["seek", "resume"]);
        harness.Seeks.ShouldBe([new TopicPartitionOffset("topic", 0, 0)]);
        harness.Resumes.ShouldBe([partition0]);
        harness.Paused.ShouldBe([partition1]);

        await harness.DeliverAsync((0, 0), (2, 0));
        await harness.WaitForCommitAsync(0, 1);
        await harness.WaitForCommitAsync(2, 1);

        // Assigned again, partition 1 is resumed by its new assignment
        await harness.PollAsync(() =>
        {
            harness.Assign(1);

            return null;
        });

        harness.Resumes.ShouldBe([partition0, partition1]);
        harness.Paused.ShouldBeEmpty();

        await harness.DeliverAsync((1, 0));
        await harness.WaitForCommitAsync(1, 1);

        harness.Completed.ShouldBe([new Delivery(0, 0, 1), new Delivery(2, 0, 1), new Delivery(1, 0, 2)], true);
        harness.UnsafeStores.ShouldBeEmpty();
        harness.UnsafeCommits.ShouldBeEmpty();
        harness.Errors.ShouldBeEmpty();

        return;

        static void HoldFirstReaderStop(ProcessingGate gate, TaskCompletionSource<bool> entered, TaskCompletionSource<bool> release)
        {
            int stoppingCount = 0;
            gate.OnReaderStopped = () =>
            {
                if (Interlocked.Increment(ref stoppingCount) != 1)
                    return;

                entered.TrySetResult(true);

                release.Task.Wait(Timeout).ShouldBeTrue();
            };
        }
    }

    [Theory]
    [InlineData("pause", false)]
    [InlineData("pause", true)]
    [InlineData("resume", false)]
    [InlineData("resume", true)]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The rollback is awaited before the harness is disposed")]
    public async Task Rebalance_ShouldKeepPartitionStateSetByUser_WhenReassignedAfterDefeatingRollbackRestart(string userAction, bool cooperative)
    {
        await using PollHarness harness = new(true, cooperative);
        await harness.StartAsync(0);

        ProcessingGate active = harness.Block(0, 0);
        active.SkipCommit = true;
        await harness.DeliverAsync((0, 0));
        await active.Started.Task.WaitAsync(Timeout);

        TaskCompletionSource<bool> entered = NewSignal();
        TaskCompletionSource<bool> release = NewSignal();
        int stoppingCount = 0;
        active.OnReaderStopped = () =>
        {
            if (Interlocked.Increment(ref stoppingCount) != 1)
                return;

            entered.TrySetResult(true);

            release.Task.Wait(Timeout).ShouldBeTrue();
        };

        TopicPartition partition = new("topic", 0);
        Task rollback = Task.Run(async () => await harness.Consumer.RollbackAsync(active.Context!.Envelope.BrokerMessageIdentifier));

        try
        {
            await entered.Task.WaitAsync(Timeout);

            // The user pauses or resumes the partition while it's paused by the rollback
            if (userAction == "pause")
                harness.Consumer.Pause([partition]);
            else
                harness.Consumer.Resume([partition]);

            Task rebalance = harness.PollAsync(() =>
            {
                harness.Revoke(0);
                harness.Assign(0);

                return null;
            });

            await PollHarness.WaitUntilAsync(() => harness.Logs.Count(log => log.StartsWith("Stopping processing loop", StringComparison.Ordinal)) >= 2);
            active.Release.TrySetResult(true);
            await rebalance.WaitAsync(Timeout);

            release.TrySetResult(true);
            await rollback.WaitAsync(Timeout);
            await PollHarness.WaitUntilAsync(() => harness.Logs.Any(log => log.StartsWith("Skipping rollback restart", StringComparison.Ordinal)));
        }
        finally
        {
            release.TrySetResult(true);
            active.Release.TrySetResult(true);
            await rollback.WaitAsync(Timeout);
        }

        // Only the user resumed the partition, if at all
        harness.Paused.ShouldBe(userAction == "pause" ? [partition] : []);
        harness.Resumes.ShouldBe(userAction == "pause" ? [] : [partition]);

        if (userAction == "pause")
            harness.Consumer.Resume([partition]);

        await harness.DeliverAsync((0, 0));
        await harness.WaitForCommitAsync(0, 1);

        harness.Paused.ShouldBeEmpty();
        harness.Resumes.ShouldBe([partition]);
        harness.UnsafeStores.ShouldBeEmpty();
        harness.UnsafeCommits.ShouldBeEmpty();
        harness.Errors.ShouldBeEmpty();
    }
}
