// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Silverback.Collections;
using Silverback.Configuration;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Messaging.Consuming.Transaction;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Sequences.Batch;
using Silverback.Messaging.Sequences.Unbounded;
using Silverback.Messaging.Serialization;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker.Kafka;

public partial class KafkaRebalanceLifecycleTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private readonly ITestOutputHelper _output;

    public KafkaRebalanceLifecycleTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Callbacks are awaited before the harness is disposed.")]
    public async Task Rebalance_ShouldDiscardOldBuffersAndPreserveOrder_WhenSamePollReturnsReassignedRecord(
        bool independent, bool cooperative)
    {
        await using PollHarness harness = new(independent, cooperative);
        await harness.StartAsync(0);
        ProcessingGate active = harness.Block(0, 0);
        await harness.DeliverAsync((0, 0), (0, 1), (0, 2));
        await active.Started.Task.WaitAsync(Timeout);

        Task rebalance = harness.PollAsync(() =>
        {
            harness.Revoke(0);
            harness.Assign(0);
            return harness.Record(0, 0);
        });
        await active.ReaderStopped.Task.WaitAsync(Timeout);
        harness.CommittedOffset(0).ShouldBe(0);
        harness.Starts.Select(item => item.Offset).ShouldBe([0L]);
        active.Release.TrySetResult(true);
        await rebalance.WaitAsync(Timeout);
        await harness.DeliverAsync((0, 1), (0, 2));
        await harness.WaitForCommitAsync(0, 3);

        // The active, uncommitted record may replay. The old queued copies must never execute.
        harness.Starts.ShouldBe([new Delivery(0, 0, 1), new Delivery(0, 0, 2), new Delivery(0, 1, 2), new Delivery(0, 2, 2)]);
        harness.UnsafeCommits.ShouldBeEmpty();
        harness.Errors.ShouldBeEmpty();
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Callbacks are awaited before the harness is disposed.")]
    public async Task CooperativeRebalance_ShouldPreserveRetainedPartitionBufferAndOrdering()
    {
        await using PollHarness harness = new(true, true);
        await harness.StartAsync(0, 1);
        ProcessingGate revoked = harness.Block(0, 0);
        ProcessingGate retained = harness.Block(1, 0);
        await harness.DeliverAsync((0, 0), (1, 0), (0, 1), (1, 1), (0, 2), (1, 2));
        await Task.WhenAll(revoked.Started.Task, retained.Started.Task).WaitAsync(Timeout);

        Task rebalance = harness.PollAsync(() =>
        {
            harness.Revoke(0);
            return harness.Record(1, 3);
        });
        await revoked.ReaderStopped.Task.WaitAsync(Timeout);
        retained.ReaderStopped.Task.IsCompleted.ShouldBeFalse();
        revoked.Release.TrySetResult(true);
        await rebalance.WaitAsync(Timeout);
        retained.Release.TrySetResult(true);
        await harness.WaitForCommitAsync(1, 4);

        harness.Starts.Where(item => item.Partition == 1).ShouldBe(
            [new Delivery(1, 0, 1), new Delivery(1, 1, 1), new Delivery(1, 2, 1), new Delivery(1, 3, 1)]);
        harness.Starts.Where(item => item.Partition == 0).ShouldBe([new Delivery(0, 0, 1)]);
        harness.CommittedOffset(0).ShouldBe(0);

        await harness.PollAsync(() =>
        {
            harness.Assign(0);
            return harness.Record(0, 0);
        });
        await harness.DeliverAsync((0, 1), (0, 2));
        await harness.WaitForCommitAsync(0, 3);
        harness.Starts.Where(item => item.Partition == 0).ShouldBe(
            [new Delivery(0, 0, 1), new Delivery(0, 0, 2), new Delivery(0, 1, 2), new Delivery(0, 2, 2)]);
        harness.UnsafeCommits.ShouldBeEmpty();
        harness.Errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Callbacks are awaited before the harness is disposed.")]
    public async Task Shutdown_ShouldRejectAssignmentAndLeaveUnfinishedRecordsReplayable(bool cooperative)
    {
        await using PollHarness harness = new(true, cooperative);
        await harness.StartAsync(0);
        ProcessingGate active = harness.Block(0, 0);
        await harness.DeliverAsync((0, 0), (0, 1), (0, 2));
        await active.Started.Task.WaitAsync(Timeout);
        int assignedDuringStop = -1;
        Task rebalance = harness.PollAsync(() =>
        {
            harness.Revoke(0);
            assignedDuringStop = harness.Assign(0);
            return null;
        });
        await active.ReaderStopped.Task.WaitAsync(Timeout);
        await harness.Consumer.StopAsync().AsTask().WaitAsync(Timeout);
        await rebalance.WaitAsync(Timeout);

        assignedDuringStop.ShouldBe(0);
        harness.Completed.ShouldBeEmpty();
        harness.CommittedOffset(0).ShouldBe(0);

        await harness.Consumer.StartAsync();
        await harness.PollAsync(() =>
        {
            harness.Assign(0);
            return harness.Record(0, 0);
        });
        await harness.DeliverAsync((0, 1), (0, 2));
        await harness.WaitForCommitAsync(0, 3);
        harness.Completed.Select(item => item.Offset).ShouldBe([0L, 1L, 2L]);
        harness.UnsafeCommits.ShouldBeEmpty();
        harness.Errors.ShouldBeEmpty();
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Callbacks are awaited before the harness is disposed.")]
    public async Task Rollback_ShouldNotSeekOrResumePartitionAlreadyBeingRevoked()
    {
        await using PollHarness harness = new();
        await harness.StartAsync(0);
        ProcessingGate active = harness.Block(0, 0);
        await harness.DeliverAsync((0, 0), (0, 1), (0, 2));
        await active.Started.Task.WaitAsync(Timeout);
        Task revoke = harness.PollAsync(() =>
        {
            harness.Revoke(0);
            return null;
        });
        await active.ReaderStopped.Task.WaitAsync(Timeout);

        await harness.Consumer.RollbackAsync(new KafkaOffset("topic", 0, 0));
        active.Release.TrySetResult(true);
        await revoke.WaitAsync(Timeout);
        harness.Seeks.ShouldBeEmpty();
        harness.Resumes.ShouldBeEmpty();
        harness.CommittedOffset(0).ShouldBe(0);

        await harness.PollAsync(() =>
        {
            harness.Assign(0);
            return harness.Record(0, 0);
        });
        await harness.DeliverAsync((0, 1), (0, 2));
        await harness.WaitForCommitAsync(0, 3);
        harness.Starts.ShouldBe([new Delivery(0, 0, 1), new Delivery(0, 0, 2), new Delivery(0, 1, 2), new Delivery(0, 2, 2)]);
        harness.UnsafeCommits.ShouldBeEmpty();
        harness.Errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Callbacks are awaited before the harness is disposed.")]
    public async Task Rollback_ShouldNotRestartObsoleteAssignment_WhenRebalanceOvertakesPendingRollback(bool independent, bool reassign)
    {
        await using PollHarness harness = new(independent);
        await harness.StartAsync(0);
        ProcessingGate active = harness.Block(0, 0);
        await harness.DeliverAsync((0, 0), (0, 1), (0, 2));
        await active.Started.Task.WaitAsync(Timeout);
        TaskCompletionSource<bool> stopEntered = NewSignal();
        TaskCompletionSource<bool> releaseStop = NewSignal();
        int stoppingCount = 0;
        active.OnReaderStopped = () =>
        {
            if (Interlocked.Increment(ref stoppingCount) != 1)
                return;
            stopEntered.TrySetResult(true);
            releaseStop.Task.Wait(Timeout).ShouldBeTrue();
        };

        Task rollback = Task.Run(async () => await harness.Consumer.RollbackAsync(new KafkaOffset("topic", 0, 0)));
        try
        {
            await stopEntered.Task.WaitAsync(Timeout);
            Task rebalance = harness.PollAsync(() =>
            {
                harness.Revoke(0);
                return null;
            });
            await PollHarness.WaitUntilAsync(() => harness.Logs.Count(text => text.StartsWith("Stopping processing loop", StringComparison.Ordinal)) >= 2);
            active.Release.TrySetResult(true);
            await rebalance.WaitAsync(Timeout);

            ProcessingGate? reassigned = null;
            if (reassign)
            {
                reassigned = harness.Block(0, 0);
                await harness.PollAsync(() =>
                {
                    harness.Assign(0);
                    return harness.Record(0, 0);
                });
                await harness.DeliverAsync((0, 1), (0, 2));
                await reassigned.Started.Task.WaitAsync(Timeout);
            }

            releaseStop.TrySetResult(true);
            await rollback.WaitAsync(Timeout);
            await harness.RollbackRestartObserved.Task.WaitAsync(Timeout);
            harness.Resumes.ShouldBeEmpty();
            if (reassigned != null)
            {
                reassigned.Release.TrySetResult(true);
                await harness.WaitForCommitAsync(0, 3);
                harness.Starts.ShouldBe([new Delivery(0, 0, 1), new Delivery(0, 0, 2), new Delivery(0, 1, 2), new Delivery(0, 2, 2)]);
            }
            else
            {
                harness.Starts.ShouldBe([new Delivery(0, 0, 1)]);
                harness.CommittedOffset(0).ShouldBe(0);
            }

            harness.UnsafeCommits.ShouldBeEmpty();
            harness.Errors.ShouldBeEmpty();
        }
        finally
        {
            releaseStop.TrySetResult(true);
            active.Release.TrySetResult(true);
            await rollback.WaitAsync(Timeout);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Rollback_ShouldResumeCurrentAssignmentAndReplayBufferedRecordsInOrder(bool independent)
    {
        await using PollHarness harness = new(independent);
        await harness.StartAsync(0);
        ProcessingGate active = harness.Block(0, 0);
        await harness.DeliverAsync((0, 0), (0, 1), (0, 2));
        await active.Started.Task.WaitAsync(Timeout);

        await harness.Consumer.RollbackAsync(new KafkaOffset("topic", 0, 0));
        await active.ReaderStopped.Task.WaitAsync(Timeout);
        active.Release.TrySetResult(true);
        await harness.RollbackRestartObserved.Task.WaitAsync(Timeout);
        harness.Seeks.ShouldBe([new TopicPartitionOffset("topic", 0, 0)]);
        harness.Resumes.ShouldBe([new TopicPartition("topic", 0)]);
        await harness.DeliverAsync((0, 0), (0, 1), (0, 2));
        await harness.WaitForCommitAsync(0, 3);

        harness.Starts.Select(item => item.Offset).ShouldBe([0L, 0L, 1L, 2L]);
        harness.UnsafeCommits.ShouldBeEmpty();
        harness.Errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Rollback_ShouldGiveReplayedOffsetNewChannelIdentity(bool independent)
    {
        await using PollHarness harness = new(independent);
        await harness.StartAsync(0);
        PartitionChannel originalChannel = harness.GetChannel(0).ShouldNotBeNull();
        ProcessingGate original = harness.Block(0, 0);
        await harness.DeliverAsync((0, 0));
        await original.Started.Task.WaitAsync(Timeout);
        KafkaOffset originalOffset = original.Context!.Envelope.BrokerMessageIdentifier.ShouldBeOfType<KafkaOffset>();
        originalOffset.BelongsToChannel(originalChannel.InstanceId).ShouldBeTrue();

        await harness.Consumer.RollbackAsync(originalOffset);
        await original.ReaderStopped.Task.WaitAsync(Timeout);
        original.Release.TrySetResult(true);
        await harness.RollbackRestartObserved.Task.WaitAsync(Timeout);
        PartitionChannel replacementChannel = harness.GetChannel(0).ShouldNotBeNull();
        replacementChannel.InstanceId.ShouldNotBe(originalChannel.InstanceId);

        ProcessingGate replay = harness.Block(0, 0);
        await harness.DeliverAsync((0, 0));
        await replay.Started.Task.WaitAsync(Timeout);
        KafkaOffset replayOffset = replay.Context!.Envelope.BrokerMessageIdentifier.ShouldBeOfType<KafkaOffset>();
        replayOffset.ShouldBe(originalOffset);
        replayOffset.BelongsToChannel(replacementChannel.InstanceId).ShouldBeTrue();
        replayOffset.BelongsToChannel(originalChannel.InstanceId).ShouldBeFalse();
        originalOffset.BelongsToChannel(originalChannel.InstanceId).ShouldBeTrue();
        originalOffset.BelongsToChannel(replacementChannel.InstanceId).ShouldBeFalse();

        await harness.Consumer.CommitAsync(originalOffset);
        await harness.Consumer.RollbackAsync(originalOffset);
        await harness.Consumer.StopAsync(originalOffset, false);
        replacementChannel.ReadCancellationToken.IsCancellationRequested.ShouldBeFalse();
        harness.CommittedOffset(0).ShouldBe(0);
        harness.Seeks.ShouldBe([new TopicPartitionOffset("topic", 0, 0)]);
        replay.Release.TrySetResult(true);
        await harness.WaitForCommitAsync(0, 1);
        harness.UnsafeCommits.ShouldBeEmpty();
        harness.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task Rollback_ShouldReplayAgain_WhenSharedTrackerStillContainsPreviousChannelOffsets()
    {
        await using PollHarness harness = new(false);
        await harness.StartAsync(0);
        await harness.DeliverAsync((0, 0));
        await harness.WaitForCommitAsync(0, 1);

        for (int iteration = 0; iteration < 2; iteration++)
        {
            ProcessingGate active = harness.Block(0, 1);
            await harness.DeliverAsync((0, 1));
            await active.Started.Task.WaitAsync(Timeout);
            PartitionChannel? previous = harness.GetChannel(0);
            await harness.Consumer.RollbackAsync(active.Context!.Envelope.BrokerMessageIdentifier);
            await active.ReaderStopped.Task.WaitAsync(Timeout);
            active.Release.TrySetResult(true);
            await PollHarness.WaitUntilAsync(() =>
                harness.GetChannel(0) is { } current && !ReferenceEquals(current, previous) &&
                harness.Resumes.Count == iteration + 1);
        }

        harness.Seeks.Select(offset => offset.Offset.Value).ShouldBe([1L, 1L]);
        await harness.DeliverAsync((0, 1), (0, 2));
        await harness.WaitForCommitAsync(0, 3);
        harness.UnsafeCommits.ShouldBeEmpty();
        harness.Errors.ShouldBeEmpty();
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Reconnect is awaited before disposing the harness.")]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task Rollback_ShouldTriggerRecovery_WhenResumingCurrentAssignmentFails()
    {
        await using PollHarness harness = new();
        await harness.StartAsync(0);
        ProcessingGate active = harness.Block(0, 0);
        await harness.DeliverAsync((0, 0));
        await active.Started.Task.WaitAsync(Timeout);
        TaskCompletionSource<bool> reconnect = NewSignal();
        harness.Client.When(client => client.Resume(Arg.Any<IEnumerable<TopicPartition>>()))
            .Do(_ => throw new InvalidOperationException("Resume failed"));
        harness.Client.ReconnectAsync().Returns(_ =>
        {
            reconnect.TrySetResult(true);
            return ValueTask.CompletedTask;
        });

        await harness.Consumer.RollbackAsync(active.Context!.Envelope.BrokerMessageIdentifier);
        await active.ReaderStopped.Task.WaitAsync(Timeout);
        active.Release.TrySetResult(true);
        await reconnect.Task.WaitAsync(Timeout);
        harness.GetChannel(0).ShouldBeNull();
        harness.UnsafeCommits.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 1)]
    [InlineData(true, 2)]
    [InlineData(false, 2)]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The harness is disposed after the test completes.")]
    public async Task StopReading_ShouldNotRemoveReplacement_WhenAnOlderStopCompletesLate(bool independent, int reassignments)
    {
        await using PollHarness harness = new(independent);
        await harness.StartAsync(0);
        ProcessingGate active = harness.Block(0, 0);
        await harness.DeliverAsync((0, 0));
        await active.Started.Task.WaitAsync(Timeout);
        TaskCompletionSource<bool> oldStopEntered = NewSignal();
        TaskCompletionSource<bool> releaseOldStop = NewSignal();
        int stopCount = 0;
        active.OnReaderStopped = () =>
        {
            if (Interlocked.Increment(ref stopCount) != 1)
                return;
            oldStopEntered.TrySetResult(true);
            releaseOldStop.Task.Wait(Timeout).ShouldBeTrue();
        };
        PartitionChannel original = harness.GetChannel(0).ShouldNotBeNull();
        List<PartitionChannel> channels = [original];
        Task oldStop = Task.Run(() => harness.Channels.StopReadingAsync(new TopicPartition("topic", 0)));
        try
        {
            await oldStopEntered.Task.WaitAsync(Timeout);
            for (int assignment = 0; assignment < reassignments; assignment++)
            {
                Task rebalance = harness.PollAsync(() =>
                {
                    harness.Revoke(0);
                    harness.Assign(0);
                    return null;
                });
                await PollHarness.WaitUntilAsync(() => Volatile.Read(ref stopCount) >= 2);
                active.Release.TrySetResult(true);
                await rebalance;

                PartitionChannel assigned = harness.GetChannel(0).ShouldNotBeNull();
                assigned.ShouldNotBeSameAs(channels[^1]);
                channels.Add(assigned);
            }

            PartitionChannel replacement = channels[^1];
            releaseOldStop.TrySetResult(true);
            await oldStop.WaitAsync(Timeout);

            // Inspect identity as well as progress: removal by key can leave an orphan reader alive.
            harness.GetChannel(0).ShouldBeSameAs(replacement);
            await harness.DeliverAsync((0, 0), (0, 1));
            await harness.WaitForCommitAsync(0, 2);
            harness.UnsafeCommits.ShouldBeEmpty();

            // Normal shutdown still removes the registered channel and stops every reader.
            await harness.Channels.StopReadingAsync().WaitAsync(Timeout);
            harness.GetChannel(0).ShouldBeNull();
            channels.ShouldAllBe(channel => channel.ReadTask.IsCompletedSuccessfully);
            harness.Errors.ShouldBeEmpty();
        }
        finally
        {
            releaseOldStop.TrySetResult(true);
            active.Release.TrySetResult(true);
            await oldStop.WaitAsync(Timeout);
            foreach (PartitionChannel channel in channels)
                await channel.StopReadingAsync().WaitAsync(Timeout);
        }
    }

    [Theory]
    [InlineData("filter", false)]
    [InlineData("filter", true)]
    [InlineData("versions", false)]
    [InlineData("versions", true)]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Callbacks are awaited before the harness is disposed.")]
    public async Task Rollback_ShouldNotMutateNewOwnership_WhenRebalanceCrossesBoundary(string boundary, bool reassign)
    {
        await using PollHarness harness = new();
        await harness.StartAsync(0);
        ProcessingGate active = harness.Block(0, 0);
        await harness.DeliverAsync((0, 0));
        await active.Started.Task.WaitAsync(Timeout);
        TaskCompletionSource<bool> entered = NewSignal();
        TaskCompletionSource<bool> release = NewSignal();
        void Hold()
        {
            entered.TrySetResult(true);
            release.Task.Wait(Timeout).ShouldBeTrue();
        }

        IReadOnlyCollection<KafkaOffset> offsets = new BoundaryOffsets(
            new KafkaOffset("topic", 0, 0), boundary == "filter" ? Hold : null, boundary == "versions" ? Hold : null);

        // Expose the protected entry point only to gate materialization; the rollback implementation is unchanged.
        Task rollback = Task.Run(async () => await harness.Consumer.RollbackCoreForTestAsync(offsets));
        try
        {
            await entered.Task.WaitAsync(Timeout);
            Task rebalance = harness.PollAsync(() =>
            {
                harness.Revoke(0);
                if (reassign)
                    harness.Assign(0);
                return null;
            });
            await active.ReaderStopped.Task.WaitAsync(Timeout);
            active.Release.TrySetResult(true);
            await rebalance;
            int mutationsBeforeRelease = harness.NativeMutations.Count;
            release.TrySetResult(true);
            await rollback.WaitAsync(Timeout);
            if (boundary != "filter" || reassign)
                await harness.RollbackRestartObserved.Task.WaitAsync(Timeout);
            harness.NativeMutations.Skip(mutationsBeforeRelease).ShouldBeEmpty(
                $"Old rollback crossed the {boundary} boundary after ownership changed");
            if (reassign)
            {
                await harness.DeliverAsync((0, 0), (0, 1));
                await harness.WaitForCommitAsync(0, 2);
            }

            harness.UnsafeCommits.ShouldBeEmpty();
        }
        finally
        {
            release.TrySetResult(true);
            active.Release.TrySetResult(true);
            await rollback.WaitAsync(Timeout);
        }
    }

    [Theory]
    [InlineData("pause", false)]
    [InlineData("pause", true)]
    [InlineData("seek", false)]
    [InlineData("seek", true)]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "All operations complete before disposing the harness.")]
    public async Task Rollback_ShouldSerializeNativeMutationWithRebalance(string boundary, bool reassign)
    {
        await using PollHarness harness = new();
        await harness.StartAsync(0);
        ProcessingGate active = harness.Block(0, 0);
        await harness.DeliverAsync((0, 0));
        await active.Started.Task.WaitAsync(Timeout);
        TaskCompletionSource<bool> entered = NewSignal();
        TaskCompletionSource<bool> release = NewSignal();
        TaskCompletionSource<bool> revoking = NewSignal();
        void Hold()
        {
            entered.TrySetResult(true);
            release.Task.Wait(Timeout).ShouldBeTrue();
        }

        if (boundary == "pause")
            harness.BeforePause = Hold;
        else
            harness.BeforeSeek = Hold;

        Task rollback = Task.Run(async () => await harness.Consumer.RollbackAsync(new KafkaOffset("topic", 0, 0)));
        Task? rebalance = null;
        try
        {
            await entered.Task.WaitAsync(Timeout);
            rebalance = harness.PollAsync(() =>
            {
                revoking.TrySetResult(true);
                harness.Revoke(0);
                if (reassign)
                    harness.Assign(0);
                return null;
            });
            await revoking.Task.WaitAsync(Timeout);

            // The callback cannot change ownership while Pause/Seek is executing under the assignment lock.
            rebalance.IsCompleted.ShouldBeFalse();
            release.TrySetResult(true);
            active.Release.TrySetResult(true);
            await rollback.WaitAsync(Timeout);
            await rebalance;
            int mutations = harness.NativeMutations.Count;
            await harness.RollbackRestartObserved.Task.WaitAsync(Timeout);
            harness.NativeMutations.Count.ShouldBe(mutations);

            if (reassign)
            {
                await harness.DeliverAsync((0, 0), (0, 1));
                await harness.WaitForCommitAsync(0, 2);
            }

            harness.UnsafeCommits.ShouldBeEmpty();
            harness.Errors.ShouldBeEmpty();
        }
        finally
        {
            release.TrySetResult(true);
            active.Release.TrySetResult(true);
            await rollback.WaitAsync(Timeout);
            if (rebalance != null)
                await rebalance.WaitAsync(Timeout);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Rollback_ShouldIgnoreOffsets_WhenConsumerHasStopped(bool ownedOffset)
    {
        await using PollHarness harness = new();
        await harness.StartAsync(0);
        ProcessingGate active = harness.Block(0, 0);
        await harness.DeliverAsync((0, 0));
        await active.Started.Task.WaitAsync(Timeout);
        KafkaOffset offset = ownedOffset
            ? (KafkaOffset)active.Context!.Envelope.BrokerMessageIdentifier
            : new KafkaOffset("topic", 0, 0);
        await harness.Consumer.StopAsync();
        await harness.Consumer.RollbackAsync(offset);
        harness.NativeMutations.ShouldBeEmpty();
        harness.GetChannel(0).ShouldBeNull();
        harness.CommittedOffset(0).ShouldBe(0);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Callbacks are awaited before the harness is disposed.")]
    public async Task Shutdown_ShouldDefeatPendingRollbackRestart(bool reconnect, bool independent)
    {
        await using PollHarness harness = new(independent);
        await harness.StartAsync(0);
        ProcessingGate active = harness.Block(0, 0);
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
        Task rollback = Task.Run(async () => await harness.Consumer.RollbackAsync(new KafkaOffset("topic", 0, 0)));
        try
        {
            await entered.Task.WaitAsync(Timeout);
            await harness.Consumer.StopAsync().AsTask().WaitAsync(Timeout);
            if (reconnect)
            {
                await harness.StartAsync(0);

                // Two successive transitions must also invalidate the previous lifecycle's continuation.
                for (int index = 0; index < 2; index++)
                {
                    await harness.PollAsync(() =>
                    {
                        harness.Revoke(0);
                        harness.Assign(0);
                        return null;
                    });
                }
            }

            release.TrySetResult(true);
            await rollback.WaitAsync(Timeout);
            await harness.RollbackRestartObserved.Task.WaitAsync(Timeout);
            harness.Resumes.ShouldBeEmpty();
            if (reconnect)
            {
                await harness.DeliverAsync((0, 0), (0, 1));
                await harness.WaitForCommitAsync(0, 2);
            }
            else
            {
                harness.CommittedOffset(0).ShouldBe(0);
                harness.GetChannel(0).ShouldBeNull();
            }

            harness.UnsafeCommits.ShouldBeEmpty();
            harness.Errors.ShouldBeEmpty();
        }
        finally
        {
            release.TrySetResult(true);
            active.Release.TrySetResult(true);
            await rollback.WaitAsync(Timeout);
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Callbacks are awaited before the harness is disposed.")]
    public async Task Shutdown_ShouldNotCommitPastBufferedRecords_WhenCanceledPollWriteOverflows(bool independent, bool autoCommit)
    {
        await using PollHarness harness = new(independent, backpressure: 2, autoCommit: autoCommit);
        await harness.StartAsync(0);
        ProcessingGate active = harness.Block(0, 0);
        active.IgnoreCancellation = true; // Subscribers may complete successfully after shutdown is requested
        await harness.DeliverAsync((0, 0));
        await active.Started.Task.WaitAsync(Timeout);
        await harness.DeliverAsync((0, 1), (0, 2));
        await harness.PollAsync(() => harness.Record(0, 3));
        PartitionChannel channel = harness.GetChannel(0)!;

        TaskCompletionSource<bool> stoppingReader = NewSignal();
        TaskCompletionSource<bool> allowReaderStop = NewSignal();
        TaskCompletionSource<bool> nextCommitted = NewSignal();
        TaskCompletionSource<bool> finishHandler = NewSignal();
        harness.BeforeChannelStop = () =>
        {
            stoppingReader.TrySetResult(true);
            allowReaderStop.Task.WaitAsync(Timeout).GetAwaiter().GetResult();
        };
        harness.AfterCommit = async delivery =>
        {
            if (delivery.Offset == 0)
                return;
            nextCommitted.TrySetResult(true);
            await finishHandler.Task.WaitAsync(Timeout);
        };

        Task stop = Task.Run(async () => await harness.Consumer.StopAsync());
        long storedBeforeStop;
        try
        {
            await stoppingReader.Task.WaitAsync(Timeout);

            // The actual polling loop must finish its canceled write and enqueue the overflow record
            await harness.PollingStopped.WaitAsync(Timeout);
            active.Release.TrySetResult(true);
            await nextCommitted.Task.WaitAsync(Timeout);
            storedBeforeStop = harness.StoredOffset(0);
            allowReaderStop.TrySetResult(true);
            await PollHarness.WaitUntilAsync(() => channel.ReadCancellationToken.IsCancellationRequested);
        }
        finally
        {
            allowReaderStop.TrySetResult(true);
            active.Release.TrySetResult(true);
            finishHandler.TrySetResult(true);
            await stop.WaitAsync(Timeout);
            harness.BeforeChannelStop = null;
            harness.AfterCommit = null;
        }

        // Simulate the automatic commit timer as well as manual commits before the next assignment
        harness.Client.Commit();
        long replayFrom = harness.CommittedOffset(0);
        await harness.StartAsync(0);
        for (long offset = replayFrom; offset < 4; offset++)
            await harness.DeliverAsync((0, offset));
        await PollHarness.WaitUntilAsync(() => harness.StoredOffset(0) == 4);
        harness.Client.Commit();

        storedBeforeStop.ShouldBe(2);
        replayFrom.ShouldBe(2);
        harness.Completed.Select(delivery => delivery.Offset).ShouldBe([0L, 1L, 2L, 3L]);
        harness.UnsafeStores.ShouldBeEmpty();
        harness.UnsafeCommits.ShouldBeEmpty();
        harness.Errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FullBuffers_ShouldStopAndReplayWithoutSkippingUnfinishedRecords(bool rollback)
    {
        await using PollHarness harness = new(backpressure: 1, parallelism: 1);
        await harness.StartAsync(0, 1, 2);
        ProcessingGate active = harness.Block(0, 0);
        await harness.DeliverAsync((0, 0));
        await active.Started.Task.WaitAsync(Timeout);

        // Two other readers now wait for the only processing slot.
        await harness.DeliverAsync((1, 0), (2, 0), (0, 1));
        await harness.PollAsync(() => harness.Record(0, 2));
        Task barrier = harness.PollAsync(() => null);
        barrier.IsCompleted.ShouldBeFalse();
        if (rollback)
        {
            await harness.Consumer.RollbackAsync(new KafkaOffset("topic", 0, 0));
            await active.ReaderStopped.Task.WaitAsync(Timeout);
            active.Release.TrySetResult(true);
            await harness.RollbackRestartObserved.Task.WaitAsync(Timeout);
        }
        else
        {
            await harness.Consumer.StopAsync().AsTask().WaitAsync(Timeout);
            harness.CommittedOffset(0).ShouldBe(0);
            await harness.StartAsync(0, 1, 2);
        }

        await barrier.WaitAsync(Timeout);
        await harness.DeliverAsync((0, 0), (0, 1), (0, 2), (1, 0), (2, 0));
        await harness.WaitForCommitAsync(0, 3);
        await harness.WaitForCommitAsync(1, 1);
        await harness.WaitForCommitAsync(2, 1);
        harness.UnsafeCommits.ShouldBeEmpty();
        harness.Errors.ShouldBeEmpty();
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Callbacks are awaited before the harness is disposed.")]
    public async Task CooperativeRebalance_ShouldPreserveOrReplayRetainedRecords_WhenSharingOneChannel()
    {
        await using PollHarness harness = new(false, true);
        await harness.StartAsync(0, 1);
        ProcessingGate active = harness.Block(0, 0);
        await harness.DeliverAsync((0, 0), (1, 0), (1, 1), (1, 2));
        await active.Started.Task.WaitAsync(Timeout);
        Task rebalance = harness.PollAsync(() =>
        {
            harness.Revoke(0);
            return null;
        });
        await active.ReaderStopped.Task.WaitAsync(Timeout);
        active.Release.TrySetResult(true);
        await rebalance;

        // A retained partition receives no new assignment callback. Replay requires an explicit seek.
        if (harness.Seeks.Any(offset => offset.Partition.Value == 1 && offset.Offset.Value == 0))
            await harness.DeliverAsync((1, 0), (1, 1), (1, 2), (1, 3));
        else
            await harness.DeliverAsync((1, 3));
        await harness.WaitForCommitAsync(1, 4);
        harness.Completed.Where(item => item.Partition == 1).Select(item => item.Offset).ShouldBe([0L, 1L, 2L, 3L]);
        harness.UnsafeCommits.ShouldBeEmpty();
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "All operations complete before disposing the harness.")]
    public async Task CooperativeRebalance_ShouldRestartRetainedPartitions_WhenPendingRollbackAlreadyRemovedSharedChannel()
    {
        await using PollHarness harness = new(false, true);
        await harness.StartAsync(0, 1);
        ProcessingGate active = harness.Block(0, 0);
        await harness.DeliverAsync((0, 0), (1, 0));
        await active.Started.Task.WaitAsync(Timeout);
        TaskCompletionSource<bool> stopEntered = NewSignal();
        using ManualResetEventSlim releaseStop = new(false);
        int stopCount = 0;
        active.OnReaderStopped = () =>
        {
            if (Interlocked.Increment(ref stopCount) != 1)
                return;
            stopEntered.TrySetResult(true);
            releaseStop.Wait(Timeout).ShouldBeTrue();
        };

        Task rollback = Task.Run(() => harness.Consumer.RollbackAsync(new KafkaOffset("topic", 0, 0)).AsTask());
        try
        {
            await stopEntered.Task.WaitAsync(Timeout);
            active.Release.TrySetResult(true);

            // A second stop finishes and removes A while the original rollback stop is still pending.
            await harness.Channels.StopReadingAsync(new TopicPartition("topic", 0)).WaitAsync(Timeout);
            harness.GetChannel(1).ShouldBeNull();
            await harness.PollAsync(() =>
            {
                harness.Revoke(0);
                return null;
            });

            PartitionChannel? replacement = harness.GetChannel(1);
            replacement.ShouldNotBeNull();
            releaseStop.Set();
            await rollback.WaitAsync(Timeout);
            await harness.RollbackRestartObserved.Task.WaitAsync(Timeout);
            harness.GetChannel(1).ShouldBeSameAs(replacement);
            harness.Seeks.ShouldContain(new TopicPartitionOffset("topic", 1, 0));
            await harness.DeliverAsync((1, 0), (1, 1));
            await harness.WaitForCommitAsync(1, 2);
            harness.UnsafeCommits.ShouldBeEmpty();
            harness.Errors.ShouldBeEmpty();
        }
        finally
        {
            active.Release.TrySetResult(true);
            releaseStop.Set();
            await rollback.WaitAsync(Timeout);
        }
    }

    [Theory]
    [InlineData(false, "rebalance", false, true)]
    [InlineData(false, "rebalance", false, false)]
    [InlineData(true, "rebalance", false, true)]
    [InlineData(true, "rebalance", false, false)]
    [InlineData(false, "shutdown", false, true)]
    [InlineData(false, "shutdown", false, false)]
    [InlineData(true, "shutdown", false, true)]
    [InlineData(true, "shutdown", false, false)]
    [InlineData(false, "reconnect", false, true)]
    [InlineData(false, "reconnect", false, false)]
    [InlineData(true, "reconnect", false, true)]
    [InlineData(true, "reconnect", false, false)]
    [InlineData(false, "rebalance", true, true)]
    [InlineData(false, "rebalance", true, false)]
    [InlineData(false, "reconnect", true, true)]
    [InlineData(false, "reconnect", true, false)]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The stream reader is awaited before sequence disposal.")]
    public async Task SequenceCleanup_ShouldNotAffectReplacement_WhenOldPartialSequenceFinishesLate(
        bool unbounded, string transition, bool commit, bool independent)
    {
        await using PollHarness harness = new(independent);
        await harness.StartAsync(0);
        ProcessingGate first = harness.Block(0, 0);
        first.SkipCommit = true;
        await harness.DeliverAsync((0, 0));
        await first.Started.Task.WaitAsync(Timeout);
        _output.WriteLine("Original record is blocked; constructing the partial sequence.");
        ConsumerPipelineContext original = first.Context!;
        InboundEnvelope envelope = new(original.Envelope, new object());
        using ConsumerPipelineContext services = ConsumerPipelineContextHelper.CreateSubstitute();
        using ConsumerPipelineContext context = new(envelope, harness.Consumer, original.SequenceStore, [], services.ServiceProvider);
        ConsumerTransactionManager transaction = new(context, Substitute.For<ISilverbackLogger<ConsumerTransactionManager>>());
        context.TransactionManager = transaction;
        using Sequence sequence = unbounded ? new UnboundedSequence("partial", context) : new BatchSequence("partial", context);
        context.SetSequence(sequence, true);
        await context.SequenceStore.AddAsync(sequence);
        TaskCompletionSource<bool> received = NewSignal();
        IMessageStreamEnumerable<IInboundEnvelope> stream = sequence.CreateStream<IInboundEnvelope>();
        Task reading = Task.Run(async () =>
        {
            try
            {
                await foreach (IInboundEnvelope item in stream)
                {
                    item.BrokerMessageIdentifier.ShouldBe(envelope.BrokerMessageIdentifier);
                    received.TrySetResult(true);
                }
            }
            catch (OperationCanceledException)
            {
                // Aborting the real sequence ends the pending stream enumeration.
            }
        });
        context.ProcessingTask = reading;
        TaskCompletionSource<bool> cleanupEntered = NewSignal();
        TaskCompletionSource<bool> releaseCleanup = NewSignal();
        Task? cleanup = null;
        Exception? failure = null;
        try
        {
            await sequence.AddAsync(envelope, null, true).WaitAsync(Timeout);
            await received.Task.WaitAsync(Timeout);
            sequence.Length.ShouldBe(1);
            sequence.IsPending.ShouldBeTrue();
            first.Release.TrySetResult(true);
            transaction.Aborting.AddHandler(async _ =>
            {
                cleanupEntered.TrySetResult(true);
                await releaseCleanup.Task.WaitAsync(Timeout);
            });

            cleanup = sequence.AbortAsync(commit ? SequenceAbortReason.IncompleteSequence : SequenceAbortReason.ConsumerAborted);
            await cleanupEntered.Task.WaitAsync(Timeout);
            _output.WriteLine("Sequence abort reached the transaction gate.");
            if (transition == "rebalance")
            {
                for (int index = 0; index < 2; index++)
                {
                    await harness.PollAsync(() =>
                    {
                        harness.Revoke(0);
                        harness.Assign(0);
                        return null;
                    });
                }
            }
            else
            {
                await harness.Consumer.StopAsync().AsTask().WaitAsync(Timeout);
                if (transition == "reconnect")
                    await harness.StartAsync(0);
            }

            _output.WriteLine("Ownership transition completed.");
            PartitionChannel? replacement = harness.GetChannel(0);
            releaseCleanup.TrySetResult(true);
            await cleanup.WaitAsync(Timeout);
            await reading.WaitAsync(Timeout);
            _output.WriteLine(
                $"Cleanup evidence: transition={transition}, unbounded={unbounded}, commit={commit}, " +
                $"committed={harness.CommittedOffset(0)}, seeks={string.Join(", ", harness.Seeks)}, " +
                $"resumes={string.Join(", ", harness.Resumes)}, replacement={replacement?.Id}, current={harness.GetChannel(0)?.Id}");
            harness.Seeks.ShouldBeEmpty("Old sequence cleanup must not seek the replacement assignment");
            harness.Resumes.ShouldBeEmpty();
            harness.CommittedOffset(0).ShouldBe(0, "The partial old batch was never successfully handled");
            harness.GetChannel(0).ShouldBeSameAs(replacement);
            if (transition != "shutdown")
            {
                await harness.DeliverAsync((0, 0), (0, 1));
                await harness.WaitForCommitAsync(0, 2);
            }

            harness.UnsafeCommits.ShouldBeEmpty();
        }
        catch (Exception exception)
        {
            failure = exception;
            _output.WriteLine($"Sequence test failure before fixture disposal: {exception}");
            _output.WriteLine($"Broker state: committed={harness.CommittedOffset(0)}, seeks={string.Join(", ", harness.Seeks)}");
            throw;
        }
        finally
        {
            first.Release.TrySetResult(true);
            releaseCleanup.TrySetResult(true);
            try
            {
                await (cleanup ?? sequence.AbortAsync(SequenceAbortReason.ConsumerAborted)).WaitAsync(Timeout);
                await reading.WaitAsync(Timeout);
            }
            catch (Exception cleanupException) when (failure != null)
            {
                _output.WriteLine($"Additional sequence cleanup failure: {cleanupException}");
            }
        }
    }

    [Theory]
    [InlineData("rebalance")]
    [InlineData("reconnect")]
    [InlineData("current")]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "All operations complete before disposing the harness.")]
    public async Task TransactionRollback_ShouldStopOnlyItsOriginalLifetime(string transition)
    {
        await using PollHarness harness = new();
        await harness.StartAsync(0);
        ProcessingGate first = harness.Block(0, 0);
        first.SkipCommit = true;
        await harness.DeliverAsync((0, 0));
        await first.Started.Task.WaitAsync(Timeout);
        using ConsumerPipelineContext context = first.Context!.Clone();
        ConsumerTransactionManager transaction = new(context, Substitute.For<ISilverbackLogger<ConsumerTransactionManager>>());
        TaskCompletionSource<bool> entered = NewSignal();
        TaskCompletionSource<bool> release = NewSignal();
        transaction.Aborting.AddHandler(async _ =>
        {
            entered.TrySetResult(true);
            await release.Task.WaitAsync(Timeout);
        });
        Task rollback = transaction.RollbackAsync(null);
        try
        {
            await entered.Task.WaitAsync(Timeout);
            first.Release.TrySetResult(true);
            if (transition == "rebalance")
            {
                await harness.PollAsync(() =>
                {
                    harness.Revoke(0);
                    harness.Assign(0);
                    return null;
                });
            }
            else if (transition == "reconnect")
            {
                await harness.Consumer.StopAsync();
                await harness.StartAsync(0);
            }

            release.TrySetResult(true);
            await rollback.WaitAsync(Timeout);
            if (transition == "current")
            {
                await PollHarness.WaitUntilAsync(() => harness.GetChannel(0) == null);
                harness.GetChannel(0).ShouldBeNull();
            }
            else
            {
                await harness.DeliverAsync((0, 0), (0, 1));
                await harness.WaitForCommitAsync(0, 2);
            }

            harness.Seeks.ShouldBeEmpty();
            harness.UnsafeCommits.ShouldBeEmpty();
            harness.Errors.ShouldBeEmpty();
        }
        finally
        {
            first.Release.TrySetResult(true);
            release.TrySetResult(true);
            await rollback.WaitAsync(Timeout);
        }
    }

    private static TaskCompletionSource<bool> NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class BoundaryOffsets(KafkaOffset offset, Action? beforeFilter, Action? beforeVersions) : IReadOnlyCollection<KafkaOffset>
    {
        public int Count => 1;

        public IEnumerator<KafkaOffset> GetEnumerator()
        {
            beforeFilter?.Invoke();
            yield return offset;
            beforeVersions?.Invoke();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class HarnessConsumer(
        IConfluentConsumerWrapper client,
        KafkaConsumerConfiguration configuration,
        IBrokerBehaviorsProvider<IConsumerBehavior> behaviors,
        ISilverbackLogger<KafkaConsumer> logger,
        IServiceProvider? serviceProvider = null)
        : KafkaConsumer("consumer", client, configuration, behaviors,
            Substitute.For<IBrokerClientCallbacksInvoker>(), Substitute.For<IKafkaOffsetStoreFactory>(),
            serviceProvider ?? Substitute.For<IServiceProvider>(), logger)
    {
        public ValueTask RollbackCoreForTestAsync(IReadOnlyCollection<KafkaOffset> offsets) => RollbackCoreAsync(offsets);
    }

    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local", Justification = "Epoch is used by record equality in assignment assertions.")]
    private sealed record Delivery(int Partition, long Offset, int Epoch);

    private sealed class ProcessingGate
    {
        public TaskCompletionSource<bool> Started { get; } = NewSignal();

        public ConsumerPipelineContext? Context { get; set; }

        public bool SkipCommit { get; set; }

        public bool IgnoreCancellation { get; set; }

        public Action? OnReaderStopped { get; set; }

        public TaskCompletionSource<bool> ReaderStopped { get; } = NewSignal();

        public TaskCompletionSource<bool> Release { get; } = NewSignal();
    }

    // Only the native client is scripted. KafkaConsumer, its polling loop, channels, and commit/rollback code are real.
    // Scripted callbacks execute synchronously on the thread calling Consume, as the Confluent client does.
    private sealed class PollHarness : IAsyncDisposable, IConsumerBehavior, ISilverbackLogger<KafkaConsumer>, ILogger
    {
        private readonly BlockingCollection<Func<ConsumeResult<byte[]?, byte[]?>?>> _polls = [];
        private readonly ConcurrentDictionary<(int Partition, long Offset), ProcessingGate> _blocked = new();
        private readonly ConcurrentBag<ProcessingGate> _gates = [];
        private readonly ConcurrentDictionary<int, int> _epochs = new();
        private readonly ConcurrentDictionary<int, long> _stored = new();
        private readonly ConcurrentDictionary<int, long> _committed = new();
        private readonly ConcurrentDictionary<(int Partition, long Offset), byte> _completed = new();
        private readonly bool _cooperative;
        private readonly ServiceProvider? _serviceProvider;
        private IReadOnlyList<TopicPartition> _assignment = [];

        public PollHarness(bool independent = true, bool cooperative = false, int backpressure = 8, int parallelism = 100, bool autoCommit = false, bool batchPipeline = false)
        {
            _cooperative = cooperative;
            Client = Substitute.For<IConfluentConsumerWrapper>();
            Client.Initialized.Returns(new AsyncEvent<BrokerClient>());
            Client.Disconnecting.Returns(new AsyncEvent<BrokerClient>());
            Client.Status.Returns(ClientStatus.Initialized);
            Client.Assignment.Returns(_ => _assignment);
            Client.Consume(Arg.Any<TimeSpan>()).Returns(call =>
                _polls.TryTake(out Func<ConsumeResult<byte[]?, byte[]?>?>? poll, call.Arg<TimeSpan>()) ? poll() : null);
            Client.When(client => client.StoreOffset(Arg.Any<TopicPartitionOffset>())).Do(call =>
            {
                TopicPartitionOffset offset = call.Arg<TopicPartitionOffset>();
                _stored[offset.Partition.Value] = offset.Offset.Value;
                for (long earlier = 0; earlier < offset.Offset.Value; earlier++)
                {
                    if (!_completed.ContainsKey((offset.Partition.Value, earlier)))
                        UnsafeStores.Enqueue(offset);
                }
            });
            Client.When(client => client.Commit()).Do(_ =>
            {
                foreach ((int partition, long offset) in _stored)
                {
                    for (long earlier = 0; earlier < offset; earlier++)
                    {
                        if (!_completed.ContainsKey((partition, earlier)))
                            UnsafeCommits.Enqueue(new TopicPartitionOffset("topic", partition, offset));
                    }

                    _committed[partition] = offset;
                }
            });
            Client.When(client => client.Seek(Arg.Any<TopicPartitionOffset>())).Do(call =>
            {
                BeforeSeek?.Invoke();
                Seeks.Enqueue(call.Arg<TopicPartitionOffset>());
                NativeMutations.Enqueue("seek");
            });
            Client.When(client => client.Pause(Arg.Any<IEnumerable<TopicPartition>>())).Do(call =>
            {
                BeforePause?.Invoke();
                if (call.Arg<IEnumerable<TopicPartition>>().Any())
                    NativeMutations.Enqueue("pause");
            });
            Client.When(client => client.Resume(Arg.Any<IEnumerable<TopicPartition>>())).Do(call =>
            {
                foreach (TopicPartition partition in call.Arg<IEnumerable<TopicPartition>>())
                {
                    Resumes.Enqueue(partition);
                    NativeMutations.Enqueue("resume");
                }
                RollbackRestartObserved.TrySetResult(true);
            });
            IBrokerBehaviorsProvider<IConsumerBehavior> behaviors = Substitute.For<IBrokerBehaviorsProvider<IConsumerBehavior>>();
            behaviors.GetBehaviorsList().Returns([this]);
            if (batchPipeline)
            {
                ServiceCollection services = new();
                services.AddLogging().AddSilverback()
                    .WithConnectionToMessageBroker(options => options.AddKafka())
                    .AddDelegateSubscriber<IAsyncEnumerable<IInboundEnvelope<string>>>(HandleBatchAsync);
                services.AddSingleton<IBrokerBehavior>(this);
                _serviceProvider = services.BuildServiceProvider();
                behaviors = _serviceProvider.GetRequiredService<IBrokerBehaviorsProvider<IConsumerBehavior>>();
            }
            Consumer = new HarnessConsumer(
                Client,
                new KafkaConsumerConfiguration
                {
                    GroupId = "tests", EnableAutoRecovery = false, EnableAutoCommit = autoCommit, CommitOffsetEach = 1,
                    ProcessPartitionsIndependently = independent, BackpressureLimit = backpressure, MaxDegreeOfParallelism = independent ? parallelism : 1,
                    PollingTimeout = TimeSpan.FromMilliseconds(10),
                    PartitionAssignmentStrategy = cooperative ? PartitionAssignmentStrategy.CooperativeSticky : null,
                    Endpoints = new ValueReadOnlyCollection<KafkaConsumerEndpointConfiguration>(
                    [
                        new KafkaConsumerEndpointConfiguration
                        {
                            Batch = new BatchSettings { Size = 10 },
                            Deserializer = new JsonMessageDeserializer<string>(),
                            TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                                [new TopicPartitionOffset("topic", Partition.Any, Offset.Unset)])
                        }
                    ])
                },
                behaviors,
                this,
                _serviceProvider);
        }

        public HarnessConsumer Consumer { get; }

        public ConsumerChannelsManager Channels =>
            (ConsumerChannelsManager)typeof(KafkaConsumer).GetField("_channelsManager", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Consumer)!;

        public Task PollingStopped =>
            ((ConsumeLoopHandler)typeof(KafkaConsumer).GetField("_consumeLoopHandler", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Consumer)!).Stopping;

        public Action? BeforeChannelStop { get; set; }

        public Func<Delivery, Task>? AfterCommit { get; set; }

        public Func<Delivery, Task>? AfterBatchRecord { get; set; }

        public ConcurrentQueue<int> CompletedBatchSizes { get; } = new();

        public Action? BeforePause { get; set; }

        public Action? BeforeSeek { get; set; }

        public ConcurrentQueue<string> NativeMutations { get; } = new();

        public IConfluentConsumerWrapper Client { get; }

        public ConcurrentQueue<Delivery> Starts { get; } = new();

        public ConcurrentQueue<Delivery> Completed { get; } = new();

        public ConcurrentQueue<TopicPartitionOffset> UnsafeCommits { get; } = new();

        public ConcurrentQueue<TopicPartitionOffset> UnsafeStores { get; } = new();

        public ConcurrentQueue<TopicPartitionOffset> Seeks { get; } = new();

        public ConcurrentQueue<TopicPartition> Resumes { get; } = new();

        public ConcurrentQueue<string> Errors { get; } = new();

        public ConcurrentQueue<string> Logs { get; } = new();

        public TaskCompletionSource<bool> RollbackRestartObserved { get; } = NewSignal();

        public int SortIndex => 0;

        public ILogger InnerLogger => this;

        public static async Task WaitUntilAsync(Func<bool> condition)
        {
            using CancellationTokenSource timeout = new(Timeout);
            while (!condition())
                await Task.Delay(1, timeout.Token);
        }

        public PartitionChannel? GetChannel(int partition)
        {
            ConcurrentDictionary<TopicPartition, PartitionChannel> channels =
                (ConcurrentDictionary<TopicPartition, PartitionChannel>)typeof(ConsumerChannelsManager)
                    .GetField("_channels", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Channels)!;
            return channels.GetValueOrDefault(Consumer.Configuration.ProcessPartitionsIndependently
                ? new TopicPartition("topic", partition) : new TopicPartition("single", Partition.Any));
        }

        public async Task StartAsync(params int[] partitions)
        {
            await Consumer.StartAsync();
            await PollAsync(() =>
            {
                Assign(partitions);
                return null;
            });
        }

        public ProcessingGate Block(int partition, long offset)
        {
            ProcessingGate gate = new();
            _blocked[(partition, offset)] = gate;
            _gates.Add(gate);
            return gate;
        }

        public Task PollAsync(Func<ConsumeResult<byte[]?, byte[]?>?> poll)
        {
            TaskCompletionSource<bool> done = NewSignal();
            _polls.Add(() =>
            {
                try
                {
                    ConsumeResult<byte[]?, byte[]?>? record = poll();
                    done.TrySetResult(true);
                    return record;
                }
                catch (Exception ex)
                {
                    done.TrySetException(ex);
                    throw;
                }
            });
            return done.Task.WaitAsync(Timeout);
        }

        public async Task DeliverAsync(params (int Partition, long Offset)[] records)
        {
            foreach ((int partition, long offset) in records)
                await PollAsync(() => Record(partition, offset));

            // The next native Consume cannot execute until the previous returned record has been enqueued.
            await PollAsync(() => null);
        }

        public int Assign(params int[] partitions)
        {
            IReadOnlyCollection<TopicPartitionOffset> accepted = Consumer.OnPartitionsAssigned(
                [.. partitions.Select(partition => new TopicPartitionOffset("topic", partition, CommittedOffset(partition)))]);
            foreach (TopicPartitionOffset offset in accepted)
                _epochs.AddOrUpdate(offset.Partition.Value, 1, (_, epoch) => epoch + 1);
            _assignment = _cooperative
                ? [.. _assignment.Concat(accepted.Select(offset => offset.TopicPartition)).Distinct()]
                : [.. accepted.Select(offset => offset.TopicPartition)];
            return accepted.Count;
        }

        public void Revoke(params int[] partitions)
        {
            Consumer.OnPartitionsRevoked([.. partitions.Select(partition => new TopicPartitionOffset("topic", partition, CommittedOffset(partition)))]);
            _assignment = [.. _assignment.Where(partition => !partitions.Contains(partition.Partition.Value))];
        }

        public ConsumeResult<byte[]?, byte[]?> Record(int partition, long offset) => new()
        {
            TopicPartitionOffset = new TopicPartitionOffset("topic", partition, offset),
            Message = new Message<byte[]?, byte[]?>
            {
                Value = _serviceProvider == null ? [1] : Encoding.UTF8.GetBytes("\"message\""),
                Headers = [new Header("test-epoch", Encoding.UTF8.GetBytes(_epochs[partition].ToString(CultureInfo.InvariantCulture)))]
            }
        };

        public long StoredOffset(int partition) => _stored.GetValueOrDefault(partition);

        public long CommittedOffset(int partition) => _committed.GetValueOrDefault(partition);

        public async Task WaitForCommitAsync(int partition, long offset)
        {
            using CancellationTokenSource timeout = new(Timeout);
            while (CommittedOffset(partition) < offset)
                await Task.Delay(1, timeout.Token);
        }

        public async ValueTask HandleAsync(ConsumerPipelineContext context, ConsumerBehaviorHandler next, CancellationToken cancellationToken)
        {
            if (_serviceProvider != null)
            {
                await HandleBatchRecordAsync(context, next, cancellationToken);
                return;
            }

            using (context)
            {
                KafkaOffset offset = (KafkaOffset)context.Envelope.BrokerMessageIdentifier;
                Delivery delivery = new(offset.TopicPartition.Partition.Value, offset.Offset, context.Envelope.Headers.GetValueOrDefault<int>("test-epoch"));
                Starts.Enqueue(delivery);
                if (_blocked.TryRemove((delivery.Partition, delivery.Offset), out ProcessingGate? gate))
                {
                    // StopReading cancels the channel token before awaiting sequences. Observe that point without sleeps
                    // or reflection into private channel state; this sequence itself has no work to await or abort.
                    ISequence observer = Substitute.For<ISequence>();
                    observer.SequenceId.Returns("stop-observer");
                    observer.Context.Returns(_ =>
                    {
                        gate.ReaderStopped.TrySetResult(true);
                        gate.OnReaderStopped?.Invoke();
                        return context;
                    });
                    await context.SequenceStore.AddAsync(observer);
                    gate.Context = context;
                    gate.Started.TrySetResult(true);
                    await gate.Release.Task.WaitAsync(Timeout, gate.IgnoreCancellation ? CancellationToken.None : cancellationToken);
                    if (gate.SkipCommit)
                        return;
                }

                Completed.Enqueue(delivery);
                _completed[(delivery.Partition, delivery.Offset)] = 0;
                await Consumer.CommitAsync(offset);
                if (AfterCommit != null)
                    await AfterCommit(delivery);
            }
        }

        public async ValueTask DisposeAsync()
        {
            foreach (ProcessingGate gate in _gates)
                gate.Release.TrySetResult(true);
            await Consumer.StopAsync().AsTask().WaitAsync(Timeout);
            await Task.Run(Consumer.Dispose).WaitAsync(Timeout);
            _polls.Dispose();
            if (_serviceProvider != null)
                await _serviceProvider.DisposeAsync();
        }

        public bool IsEnabled(LogEvent logEvent) => true;

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => null;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            string text = formatter(state, exception);
            Logs.Enqueue(text);
            if (text.StartsWith("Stopping processing loop of channel", StringComparison.Ordinal))
                BeforeChannelStop?.Invoke();
            if (text.StartsWith("Skipping rollback restart", StringComparison.Ordinal))
                RollbackRestartObserved.TrySetResult(true);
            if (logLevel >= LogLevel.Error)
                Errors.Enqueue(formatter(state, exception));
        }

        private async ValueTask HandleBatchRecordAsync(ConsumerPipelineContext context, ConsumerBehaviorHandler next, CancellationToken cancellationToken)
        {
            KafkaOffset offset = (KafkaOffset)context.Envelope.BrokerMessageIdentifier;
            Delivery delivery = new(offset.TopicPartition.Partition.Value, offset.Offset, context.Envelope.Headers.GetValueOrDefault<int>("test-epoch"));
            if (_blocked.TryRemove((delivery.Partition, delivery.Offset), out ProcessingGate? gate))
            {
                gate.Context = context;
                gate.Started.TrySetResult(true);
                await gate.Release.Task.WaitAsync(Timeout, gate.IgnoreCancellation ? CancellationToken.None : cancellationToken);
            }

            await next(context, cancellationToken);
            if (AfterBatchRecord != null)
                await AfterBatchRecord(delivery);
        }

        private async Task HandleBatchAsync(IAsyncEnumerable<IInboundEnvelope<string>> batch)
        {
            int count = 0;
            await foreach (IInboundEnvelope<string> envelope in batch)
            {
                KafkaOffset offset = (KafkaOffset)envelope.BrokerMessageIdentifier;
                Delivery delivery = new(offset.TopicPartition.Partition.Value, offset.Offset, envelope.Headers.GetValueOrDefault<int>("test-epoch"));
                Starts.Enqueue(delivery);
                Completed.Enqueue(delivery);
                _completed[(delivery.Partition, delivery.Offset)] = 0;
                count++;
            }

            CompletedBatchSizes.Enqueue(count);
        }
    }
}
