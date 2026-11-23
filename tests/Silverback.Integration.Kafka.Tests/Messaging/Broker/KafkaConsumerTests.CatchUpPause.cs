// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Sequences;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker;

public partial class KafkaConsumerTests
{
    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task EndOfPartition_ShouldPausePartitionOnceAndDisconnect_WhenCaughtUpInDisconnectMode()
    {
        await using LifecycleHarness harness = new(configuration => configuration with { CatchUpMode = CatchUpMode.Disconnect });
        TaskCompletionSource<bool> disconnected = NewSignal();
        harness.Client.DisconnectAsync().Returns(_ =>
        {
            disconnected.TrySetResult(true);

            return ValueTask.CompletedTask;
        });

        TaskCompletionSource<bool> processing = NewSignal();
        TaskCompletionSource<bool> release = NewSignal();
        harness.OnMessage = async (_, _) =>
        {
            processing.TrySetResult(true);
            await release.Task.WaitAsync(Timeout, CancellationToken.None);
        };

        await harness.StartAsync(0);
        await harness.DeliverAsync((0, 0));
        await processing.Task.WaitAsync(Timeout);

        try
        {
            await harness.DeliverEndOfPartitionAsync(0, 1);

            harness.Pauses.ShouldBe([LifecycleHarness.ToTopicPartition(0)]);
            harness.IsPartitionCaughtUp(0).ShouldBeFalse();
        }
        finally
        {
            release.TrySetResult(true);
        }

        // The record read last isn't committed by the subscriber: it's never committed, like a record filtered out
        await disconnected.Task.WaitAsync(Timeout);
        await harness.Consumer.WaitUntilCaughtUpAsync().WaitAsync(Timeout);

        harness.Pauses.ShouldBe([LifecycleHarness.ToTopicPartition(0)]);
        await harness.CaughtUpCallback.Received(1).OnConsumerCaughtUpAsync(harness.Consumer);
        harness.HasLog("Consumer caught up").ShouldBeTrue();
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task EndOfPartition_ShouldNotifyWithoutPausingOrDisconnecting_WhenCaughtUpInKeepConsumingMode()
    {
        await using LifecycleHarness harness = new(configuration => configuration with { CatchUpMode = CatchUpMode.KeepConsuming });

        // Completed synchronously by the test, which therefore runs the rest of the notification before going on
        TaskCompletionSource callbackCompletion = new();
        harness.CaughtUpCallback.OnConsumerCaughtUpAsync(harness.Consumer).Returns(callbackCompletion.Task);

        await harness.StartAsync(0);
        await harness.DeliverAsync((0, 0));
        await LifecycleHarness.WaitUntilAsync(() => harness.HandlerCommits.Count == 1);
        await harness.DeliverEndOfPartitionAsync(0, 1);

        await harness.Consumer.WaitUntilCaughtUpAsync().WaitAsync(Timeout);
        await LifecycleHarness.WaitUntilAsync(() => harness.CaughtUpCallback.ReceivedCalls().Any());
        callbackCompletion.SetResult();

        harness.Pauses.ShouldBeEmpty();
        _ = harness.Client.DidNotReceive().DisconnectAsync();
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task CaughtUp_ShouldDisconnect_WhenCallbackFails()
    {
        await using LifecycleHarness harness = new(configuration => configuration with { CatchUpMode = CatchUpMode.Disconnect });
        TaskCompletionSource<bool> disconnected = NewSignal();
        harness.Client.DisconnectAsync().Returns(_ =>
        {
            disconnected.TrySetResult(true);

            return ValueTask.CompletedTask;
        });

        harness.CaughtUpCallback.OnConsumerCaughtUpAsync(harness.Consumer).Returns(Task.FromException(new InvalidOperationException("Callback failed")));

        await harness.StartAsync(0);
        await harness.DeliverEndOfPartitionAsync(0, 0);

        await disconnected.Task.WaitAsync(Timeout);
        await harness.Consumer.WaitUntilCaughtUpAsync().WaitAsync(Timeout);

        await harness.CaughtUpCallback.Received(1).OnConsumerCaughtUpAsync(harness.Consumer);
        _ = harness.Client.Received(1).DisconnectAsync();
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task CaughtUp_ShouldLogError_WhenDisconnectFails()
    {
        await using LifecycleHarness harness = new(configuration => configuration with { CatchUpMode = CatchUpMode.Disconnect });
        harness.Client.DisconnectAsync().Returns(_ => ValueTask.FromException(new InvalidOperationException("Disconnect failed")));

        await harness.StartAsync(0);
        await harness.DeliverEndOfPartitionAsync(0, 0);

        await LifecycleHarness.WaitUntilAsync(() => harness.HasLog("Failed to disconnect the caught-up consumer"));

        harness.Errors.ShouldHaveSingleItem().ShouldStartWith("Failed to disconnect the caught-up consumer");
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task CaughtUp_ShouldNotDisconnect_WhenReconnectedWhileInvokingCallback()
    {
        await using LifecycleHarness harness = new(configuration => configuration with { CatchUpMode = CatchUpMode.Disconnect });

        // Completed synchronously by the test, which therefore runs the rest of the notification before going on
        TaskCompletionSource callbackCompletion = new();
        harness.CaughtUpCallback.OnConsumerCaughtUpAsync(harness.Consumer).Returns(callbackCompletion.Task);

        await harness.StartAsync(0);
        await harness.DeliverEndOfPartitionAsync(0, 0);

        await harness.Consumer.WaitUntilCaughtUpAsync().WaitAsync(Timeout);
        await LifecycleHarness.WaitUntilAsync(() => harness.CaughtUpCallback.ReceivedCalls().Any());

        // A new native consumer starts a new catch-up
        await harness.Consumer.StopAsync().AsTask().WaitAsync(Timeout);
        await harness.RaiseClientInitializedAsync();
        callbackCompletion.SetResult();

        _ = harness.Client.DidNotReceive().DisconnectAsync();
        harness.Consumer.WaitUntilCaughtUpAsync().IsCompleted.ShouldBeFalse();
    }

    [Theory]
    [InlineData(ClientStatus.Initialized, true)]
    [InlineData(ClientStatus.Disconnecting, false)]
    public async Task EndOfPartition_ShouldPausePartitionOnlyIfClientIsInitialized_WhenCatchingUpToDisconnect(ClientStatus status, bool expectedPaused)
    {
        await using LifecycleHarness harness = new(configuration => configuration with { CatchUpMode = CatchUpMode.Disconnect });
        await harness.StartAsync(0, 1);

        harness.Status = status;
        await harness.DeliverEndOfPartitionAsync(0, 0);

        harness.Pauses.ShouldBe(expectedPaused ? [LifecycleHarness.ToTopicPartition(0)] : []);
    }

    [Fact]
    public async Task ReadingLastRecord_ShouldMoveEndAfterLastRecordInFlight_WhenNoSequenceIsPending()
    {
        await using LifecycleHarness harness = new(configuration => configuration with { CatchUpMode = CatchUpMode.KeepConsuming });
        await harness.StartAsync(0);
        PollLifecycleRecordsAndEnd(harness, 8);

        // The records after the ones held by the sequence (5 to 7) are never committed, e.g. filtered out
        PartitionChannel channel = harness.GetChannel(0);
        await channel.SequenceStore.AddAsync(
            CreateLifecycleSequence(
                isPending: false,
                rollbackOffsets: [],
                commitOffsets:
                [
                    new KafkaOffset("topic", 0, 2),
                    new KafkaOffset("topic", 0, 4),
                    new KafkaOffset("topic", 1, 9),
                    new KafkaOffset(LifecycleHarness.ToTopicPartition(0), Offset.Unset)
                ]));

        await harness.Consumer.OnRecordReadAsync(channel, LifecycleHarness.ToTopicPartition(0), 7);
        await harness.Consumer.CommitAsync(new KafkaOffset("topic", 0, 3));

        harness.IsPartitionCaughtUp(0).ShouldBeFalse();

        await harness.Consumer.CommitAsync(new KafkaOffset("topic", 0, 4));

        harness.IsPartitionCaughtUp(0).ShouldBeTrue();
        await harness.Consumer.WaitUntilCaughtUpAsync().WaitAsync(Timeout);
    }

    [Fact]
    public async Task ReadingLastRecord_ShouldMoveEndBeforeIncompleteSequence_WhenNothingIsCommittedOrInFlight()
    {
        await using LifecycleHarness harness = new(configuration => configuration with { CatchUpMode = CatchUpMode.KeepConsuming });
        await harness.StartAsync([new TopicPartitionOffset("topic", 0, Offset.Unset)]);
        PollLifecycleRecordsAndEnd(harness, 6);

        PartitionChannel channel = harness.GetChannel(0);
        await channel.SequenceStore.AddAsync(
            CreateLifecycleSequence(
                isPending: true,
                rollbackOffsets: [new KafkaOffset("topic", 0, 3), new KafkaOffset("topic", 0, 5)],
                commitOffsets: []));

        await harness.Consumer.OnRecordReadAsync(channel, LifecycleHarness.ToTopicPartition(0), 5);
        await harness.Consumer.CommitAsync(new KafkaOffset("topic", 0, 1));

        harness.IsPartitionCaughtUp(0).ShouldBeFalse();

        await harness.Consumer.CommitAsync(new KafkaOffset("topic", 0, 2));

        harness.IsPartitionCaughtUp(0).ShouldBeTrue();
        await harness.Consumer.WaitUntilCaughtUpAsync().WaitAsync(Timeout);
    }

    [Fact]
    public async Task ReadingLastRecord_ShouldMoveEndBeforeIncompleteParentSequence_WhenEarlierRecordsAreCommittedOrInFlight()
    {
        await using LifecycleHarness harness = new(configuration => configuration with { CatchUpMode = CatchUpMode.KeepConsuming });
        await harness.StartAsync(0);
        PollLifecycleRecords(harness, 7);
        await harness.Consumer.CommitAsync(new KafkaOffset("topic", 0, 1));
        harness.Consumer.OnEndOfPartitionPolled(LifecycleHarness.ToTopicPartition(0));

        // The pending child sequence doesn't complete before its parent, which holds the records from 4 on
        ISequence parent = CreateLifecycleSequence(
            isPending: true,
            rollbackOffsets: [new KafkaOffset("topic", 0, 4), new KafkaOffset("topic", 0, 6)],
            commitOffsets: []);
        PartitionChannel channel = harness.GetChannel(0);
        await channel.SequenceStore.AddAsync(CreateLifecycleSequence(isPending: true, rollbackOffsets: [], commitOffsets: [], parent));
        await channel.SequenceStore.AddAsync(
            CreateLifecycleSequence(
                isPending: false,
                rollbackOffsets: [],
                commitOffsets: [new KafkaOffset("topic", 0, 2), new KafkaOffset("topic", 0, 5)]));

        await harness.Consumer.OnRecordReadAsync(channel, LifecycleHarness.ToTopicPartition(0), 6);

        harness.IsPartitionCaughtUp(0).ShouldBeFalse();

        await harness.Consumer.CommitAsync(new KafkaOffset("topic", 0, 2));

        harness.IsPartitionCaughtUp(0).ShouldBeTrue();
        await harness.Consumer.WaitUntilCaughtUpAsync().WaitAsync(Timeout);
    }

    [Theory]
    [InlineData("replaced")]
    [InlineData("stopped")]
    [InlineData("reset")]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The sequence is read before the channel is disposed")]
    public async Task ReadingLastRecord_ShouldBeIgnored_WhenReadByStaleChannel(string staleness)
    {
        await using LifecycleHarness harness = new(configuration => configuration with { CatchUpMode = CatchUpMode.KeepConsuming });
        await harness.StartAsync(0);
        PollLifecycleRecordsAndEnd(harness, 3);

        // Only the replaced channel isn't registered: the stopped and the reset ones are still the channel of the partition
        using PartitionChannel replacedChannel = new(8, LifecycleHarness.ToTopicPartition(0), harness);
        PartitionChannel staleChannel = staleness == "replaced" ? replacedChannel : harness.GetChannel(0);

        switch (staleness)
        {
            case "stopped":
                // Stopped, but not removed yet (as by StopChannelAsync)
                await staleChannel.StopReadingAsync().WaitAsync(Timeout);
                break;
            case "reset":
                // Reset while being read (once: the reset disposes the store, reading the sequence again)
                ISequence sequence = CreateLifecycleSequence(isPending: false, rollbackOffsets: [], commitOffsets: []);
                bool isReset = false;
                sequence.IsPending.Returns(_ =>
                {
                    if (!isReset)
                    {
                        isReset = true;
                        staleChannel.Reset();
                    }

                    return false;
                });
                await staleChannel.SequenceStore.AddAsync(sequence);
                break;
        }

        await harness.Consumer.OnRecordReadAsync(staleChannel, LifecycleHarness.ToTopicPartition(0), 2);

        harness.IsPartitionCaughtUp(0).ShouldBeFalse();

        // The records polled and not committed by the current channel are never committed (no sequence holds them). The reset channel
        // is current again with its new identity, while the stopped one can't read anymore.
        if (staleness != "stopped")
        {
            await harness.Consumer.OnRecordReadAsync(harness.GetChannel(0), LifecycleHarness.ToTopicPartition(0), 2);

            harness.IsPartitionCaughtUp(0).ShouldBeTrue();
        }
    }

    [Fact]
    public async Task Restart_ShouldResumePartitionPausedAtEnd_WhenItIsNotCaughtUp()
    {
        await using LifecycleHarness harness = new(configuration => configuration with { CatchUpMode = CatchUpMode.Disconnect });
        TaskCompletionSource<bool> processing = NewSignal();
        TaskCompletionSource<bool> neverReleased = NewSignal();
        harness.OnMessage = async (_, cancellationToken) =>
        {
            processing.TrySetResult(true);
            await neverReleased.Task.WaitAsync(Timeout, cancellationToken);
        };

        await harness.StartAsync(0, 1);
        await harness.DeliverAsync((0, 0));
        await processing.Task.WaitAsync(Timeout);
        await harness.DeliverEndOfPartitionAsync(0, 1);

        harness.Paused.ShouldBe([LifecycleHarness.ToTopicPartition(0)]);

        // The stop cancels the processing of the record, which isn't committed
        await harness.Consumer.StopAsync().AsTask().WaitAsync(Timeout);
        await harness.Consumer.StartAsync();
        await harness.PollAsync(() => null);

        harness.IsPartitionCaughtUp(0).ShouldBeFalse();
        harness.Seeks.ShouldContain(new TopicPartitionOffset("topic", 0, 0));
        harness.Resumes.ShouldBe([LifecycleHarness.ToTopicPartition(0)]);
        harness.Paused.ShouldBeEmpty();
    }

    [Fact]
    public async Task Rollback_ShouldKeepPartitionPausedAtEnd_WhenReplayingCaughtUpPartition()
    {
        await using LifecycleHarness harness = await StartLifecycleHarnessWithCaughtUpPartitionAsync();
        KafkaOffset offset = harness.HandlerCommits.ShouldHaveSingleItem();

        await harness.Consumer.RollbackAsync(offset);
        await LifecycleHarness.WaitUntilAsync(() => !harness.Seeks.IsEmpty);

        // Waits for the restart, which seeks while holding the lock
        harness.Consumer.GetStorableOffsets([], false).ShouldBeEmpty();

        harness.Seeks.ShouldBe([new TopicPartitionOffset("topic", 0, 0)]);
        harness.Resumes.ShouldBeEmpty();
        harness.Paused.ShouldBe([LifecycleHarness.ToTopicPartition(0)]);
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The poll is awaited before the harness is disposed")]
    public async Task Restart_ShouldKeepPartitionPausedAtEnd_WhenCaughtUpPartitionWasPausedByDefeatedRollback()
    {
        await using LifecycleHarness harness = await StartLifecycleHarnessWithCaughtUpPartitionAsync();
        KafkaOffset offset = harness.HandlerCommits.ShouldHaveSingleItem();

        TaskCompletionSource<bool> pollEntered = NewSignal();
        TaskCompletionSource<bool> releasePoll = NewSignal();
        Task poll = harness.PollAsync(() =>
        {
            pollEntered.TrySetResult(true);
            releasePoll.Task.Wait(Timeout).ShouldBeTrue();

            return null;
        });
        Task stopping;

        try
        {
            await pollEntered.Task.WaitAsync(Timeout);

            // The rollback restart waits for the pending poll, while the stop defeats it
            await harness.Consumer.RollbackAsync(offset);
            stopping = harness.Consumer.StopAsync().AsTask();
        }
        finally
        {
            releasePoll.TrySetResult(true);
        }

        await poll;
        await stopping.WaitAsync(Timeout);
        await LifecycleHarness.WaitUntilAsync(() => harness.HasLog("Skipping rollback restart"));

        await harness.Consumer.StartAsync();
        await harness.PollAsync(() => null);

        harness.Resumes.ShouldBeEmpty();
        harness.Paused.ShouldBe([LifecycleHarness.ToTopicPartition(0)]);
    }

    // Partition 0 is caught up and paused at its end, while partition 1 is still catching up
    private static async Task<LifecycleHarness> StartLifecycleHarnessWithCaughtUpPartitionAsync()
    {
        LifecycleHarness harness = new(configuration => configuration with { CatchUpMode = CatchUpMode.Disconnect });

        try
        {
            await harness.StartAsync(0, 1);
            await harness.DeliverAsync((0, 0));
            await LifecycleHarness.WaitUntilAsync(() => harness.HandlerCommits.Count == 1);
            await harness.DeliverEndOfPartitionAsync(0, 1);

            harness.IsPartitionCaughtUp(0).ShouldBeTrue();
            harness.IsPartitionCaughtUp(1).ShouldBeFalse();
            harness.Paused.ShouldBe([LifecycleHarness.ToTopicPartition(0)]);

            return harness;
        }
        catch (Exception)
        {
            await harness.DisposeAsync();
            throw;
        }
    }

    private static void PollLifecycleRecords(LifecycleHarness harness, int count)
    {
        for (int offset = 0; offset < count; offset++)
        {
            harness.Consumer.OnRecordPolled(new TopicPartitionOffset("topic", 0, offset));
        }
    }

    private static void PollLifecycleRecordsAndEnd(LifecycleHarness harness, int count)
    {
        PollLifecycleRecords(harness, count);
        harness.Consumer.OnEndOfPartitionPolled(LifecycleHarness.ToTopicPartition(0));
    }

    private static ISequence CreateLifecycleSequence(
        bool isPending,
        IReadOnlyCollection<IBrokerMessageIdentifier> rollbackOffsets,
        IReadOnlyCollection<IBrokerMessageIdentifier> commitOffsets,
        ISequence? parent = null)
    {
        ISequence sequence = Substitute.For<ISequence>();
        sequence.SequenceId.Returns(Guid.NewGuid().ToString());
        sequence.IsPending.Returns(isPending);
        sequence.ParentSequence.Returns(parent);
        sequence.GetRollbackIdentifiers().Returns(rollbackOffsets);
        sequence.GetCommitIdentifiers().Returns(commitOffsets);

        return sequence;
    }
}
