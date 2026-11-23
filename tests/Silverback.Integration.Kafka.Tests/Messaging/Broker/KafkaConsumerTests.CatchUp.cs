// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Configuration.Kafka;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker;

public partial class KafkaConsumerTests
{
    [Theory]
    [InlineData(CatchUpMode.Disconnect, 1)]
    [InlineData(CatchUpMode.KeepConsuming, 0)]
    public async Task OnEndOfPartitionPolled_ShouldPausePartitionOnceIfDisconnectingWhenCaughtUp_WhenEndIsPolledTwice(CatchUpMode mode, int expectedPauses)
    {
        await using PipelineHarness harness = new(mode);
        await harness.StartAsync(0);

        TaskCompletionSource<bool> gate = harness.Block(0, 0);
        await harness.DeliverAsync((0, 0));
        await harness.DeliverEndOfPartitionAsync(0, 1);
        await harness.DeliverEndOfPartitionAsync(0, 1);

        harness.CountPauses(PipelineHarness.Partition0).ShouldBe(expectedPauses);
        harness.IsPartitionCaughtUp(0).ShouldBeFalse();

        gate.TrySetResult(true);
        await harness.WaitUntilCaughtUpAsync();

        if (mode == CatchUpMode.Disconnect)
            await harness.Disconnected.Task.WaitAsync(Timeout);

        harness.CountPauses(PipelineHarness.Partition0).ShouldBe(expectedPauses);
        harness.StoredOffset(0).ShouldBe(1);
        harness.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task OnRecordReadAsync_ShouldCatchUp_WhenLastRecordBeforeEndIsNeverCommitted()
    {
        await using PipelineHarness harness = new(CatchUpMode.Disconnect);
        await harness.StartAsync(0);

        harness.SkipCommit(0, 1);
        TaskCompletionSource<bool> gate = harness.Block(0, 1);
        await harness.DeliverAsync((0, 0), (0, 1));
        await harness.DeliverEndOfPartitionAsync(0, 2);

        harness.CountPauses(PipelineHarness.Partition0).ShouldBe(1);

        // The record read last is never committed (e.g. filtered out): the end is moved before it
        gate.TrySetResult(true);
        await harness.WaitUntilCaughtUpAsync();
        await harness.Disconnected.Task.WaitAsync(Timeout);

        harness.Handled.ShouldBe([(0, 0L), (0, 1L)]);
        harness.StoredOffset(0).ShouldBe(1);
        harness.CountPauses(PipelineHarness.Partition0).ShouldBe(1);
        harness.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task OnRecordReadAsync_ShouldCatchUp_WhenEndOfPartitionIsReadAfterLastRecordThatIsNeverCommitted()
    {
        await using PipelineHarness harness = new(CatchUpMode.KeepConsuming, independent: false);
        await harness.StartAsync(0, 1);

        harness.SkipCommit(0, 1);
        await harness.DeliverAsync((0, 0), (0, 1), (1, 0));

        // The partitions share the reader: the last record of partition 0 is read before its end is polled
        await PollHarness.WaitUntilAsync(() => harness.Handled.Contains((1, 0L)));
        await harness.DeliverEndOfPartitionAsync(0, 2);
        await harness.DeliverEndOfPartitionAsync(1, 1);

        // Partition 0 can only be caught up when its end is read, since its last record is never committed
        await harness.WaitUntilCaughtUpAsync();

        harness.StoredOffset(0).ShouldBe(1);
        harness.StoredOffset(1).ShouldBe(1);
        harness.Pauses.ShouldBeEmpty();
        harness.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task OnRecordReadAsync_ShouldCompletePendingBatch_WhenLastRecordBeforeEndIsRead()
    {
        await using PipelineHarness harness = new(CatchUpMode.Disconnect, PipelineHarnessSubscriber.Batch);
        await harness.StartAsync(0);

        TaskCompletionSource<bool> gate = harness.Block(0, 1);
        await harness.DeliverAsync((0, 0), (0, 1));
        await harness.DeliverEndOfPartitionAsync(0, 2);

        harness.CompletedSequenceSizes.ShouldBeEmpty();

        // The batch of 10 is completed by the end of the partition
        gate.TrySetResult(true);
        await harness.WaitUntilCaughtUpAsync();
        await harness.Disconnected.Task.WaitAsync(Timeout);

        harness.CompletedSequenceSizes.ShouldBe([2]);
        harness.StoredOffset(0).ShouldBe(2);
        harness.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task OnRecordReadAsync_ShouldNotCatchUpBeforeBatchIsAborted_WhenPendingBatchFailedAtEndOfPartition()
    {
        await using PipelineHarness harness = new(CatchUpMode.Disconnect, PipelineHarnessSubscriber.FailingBatch);
        await harness.StartAsync(0);

        TaskCompletionSource<bool> gate = harness.Block(0, 0);
        harness.HoldUntilStreamsAborted(0, 0);
        await harness.DeliverAsync((0, 0));
        await harness.DeliverEndOfPartitionAsync(0, 1);

        // One of its subscribers failed, so the batch can't be completed early: it's pending until the other subscriber returns,
        // since only then it's aborted
        gate.TrySetResult(true);
        await harness.WaitUntilEndOfPartitionIsReadAsync(0, 1);

        harness.IsPartitionCaughtUp(0).ShouldBeFalse();
        harness.StoredOffset(0).ShouldBe(0);

        // The aborted batch is skipped by the error policy, which commits it
        harness.SecondSubscriberRelease.TrySetResult(true);
        await harness.WaitUntilCaughtUpAsync();
        await harness.Disconnected.Task.WaitAsync(Timeout);

        harness.StoredOffset(0).ShouldBe(1);
        harness.IsHoldingStreamAborted.ShouldBeTrue();
    }

    [Fact]
    public async Task OnRecordReadAsync_ShouldCatchUp_WhenStreamIsPendingAtEndOfPartition()
    {
        await using PipelineHarness harness = new(CatchUpMode.Disconnect, PipelineHarnessSubscriber.Stream);
        await harness.StartAsync(0);

        harness.SkipCommit(0, 1);
        TaskCompletionSource<bool> gate = harness.Block(0, 1);
        await harness.DeliverAsync((0, 0), (0, 1));
        await harness.DeliverEndOfPartitionAsync(0, 2);

        // The pending stream doesn't complete at the end of the partition: the partition is caught up once its records are read,
        // even though the last one isn't committed
        gate.TrySetResult(true);
        await harness.WaitUntilCaughtUpAsync();
        await harness.Disconnected.Task.WaitAsync(Timeout);

        harness.Subscribed.ShouldBe([(0, 0L)]);
        harness.CompletedSequenceSizes.ShouldBeEmpty();
        harness.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task OnRecordReadAsync_ShouldNotCatchUpBeforeReplay_WhenPartitionIsSoughtToBeginningWhileLastRecordIsRead()
    {
        await using PipelineHarness harness = new(CatchUpMode.Disconnect);
        await harness.StartAsync(0);

        harness.SkipCommit(0, 1);
        TaskCompletionSource<bool> gate = harness.Block(0, 1);
        await harness.DeliverAsync((0, 0), (0, 1));
        await harness.DeliverEndOfPartitionAsync(0, 2);

        // The commit of the first record would move the position again
        await PollHarness.WaitUntilAsync(() => harness.StoredOffset(0) == 1);

        // No offset is known after seeking to the beginning: nothing is committed until the records are polled again
        harness.Consumer.Seek(new TopicPartitionOffset(PipelineHarness.Partition0, Offset.Beginning));
        gate.TrySetResult(true);
        await harness.WaitUntilEndOfPartitionIsReadAsync(0, 2);

        harness.Handled.Count.ShouldBe(2);
        harness.IsPartitionCaughtUp(0).ShouldBeFalse();

        await harness.DeliverAsync((0, 0), (0, 1));
        await harness.WaitUntilCaughtUpAsync();
        await harness.Disconnected.Task.WaitAsync(Timeout);

        harness.Seeks.ShouldBe([new TopicPartitionOffset(PipelineHarness.Partition0, Offset.Beginning)]);
        harness.Handled.ShouldBe([(0, 0L), (0, 1L), (0, 0L), (0, 1L)]);
        harness.Errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("stop")]
    [InlineData("restart")]
    [InlineData("rollback")]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The callback runs before the harness is disposed.")]
    public async Task OnRecordReadAsync_ShouldNotCatchUpBeforeReplay_WhenReadingIsInterruptedWhileCheckingLastRecord(string interruption)
    {
        await using PipelineHarness harness = new(CatchUpMode.Disconnect);
        await harness.StartAsync(0);

        harness.SkipCommit(0, 1);
        TaskCompletionSource<bool> gate = harness.Block(0, 1);
        harness.BeforeCatchUpUpdate(0, 1, offset =>
        {
            switch (interruption)
            {
                case "stop":
                    harness.Consumer.StopAsync(false).AsTask().Wait(Timeout).ShouldBeTrue();
                    break;
                case "restart":
                    // The non-blocking stop and the start replace the channel whose reader is still running
                    harness.Consumer.StopAsync(false).AsTask().Wait(Timeout).ShouldBeTrue();
                    harness.Consumer.StartAsync().AsTask().Wait(Timeout).ShouldBeTrue();
                    SpinWait.SpinUntil(() => harness.Resumes.Contains(PipelineHarness.Partition0), Timeout).ShouldBeTrue();
                    break;
                case "rollback":
                    harness.Consumer.RollbackAsync(offset).AsTask().Wait(Timeout).ShouldBeTrue();
                    break;
            }
        });

        await harness.DeliverAsync((0, 0), (0, 1));
        await harness.DeliverEndOfPartitionAsync(0, 2);
        gate.TrySetResult(true);

        if (interruption == "stop")
        {
            // Restarted once the reader stopped by the interruption exited
            await PollHarness.WaitUntilAsync(() => harness.Logs.Any(log =>
                log.Message.StartsWith("Exited processing loop of channel topic[0]", StringComparison.Ordinal)));
            await harness.Consumer.StartAsync().AsTask().WaitAsync(Timeout);
        }

        // The partition paused at its end is resumed (by the restart or by the rollback), since it isn't caught up: the uncommitted
        // record is consumed again
        await PollHarness.WaitUntilAsync(() => harness.Resumes.Contains(PipelineHarness.Partition0));
        await harness.PollAsync(() => null);

        harness.Seeks.ShouldBe([new TopicPartitionOffset(PipelineHarness.Partition0, 1)]);
        harness.IsPartitionCaughtUp(0).ShouldBeFalse();

        await harness.DeliverAsync((0, 1));
        await harness.WaitUntilCaughtUpAsync();
        await harness.Disconnected.Task.WaitAsync(Timeout);

        // Paused again at its end, once the record is consumed again
        harness.Pauses.Last().ShouldBe(PipelineHarness.Partition0);
        harness.CountPauses(PipelineHarness.Partition0).ShouldBe(interruption == "rollback" ? 3 : 2);
        harness.Handled.ShouldBe([(0, 0L), (0, 1L), (0, 1L)]);
        harness.StoredOffset(0).ShouldBe(1);
        harness.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task OnRecordReadAsync_ShouldIgnoreRecord_WhenReadByChannelThatIsNotCurrent()
    {
        await using PipelineHarness harness = new(CatchUpMode.Disconnect);
        await harness.StartAsync(0);

        harness.SkipCommit(0, 1);
        TaskCompletionSource<bool> gate = harness.Block(0, 1);
        await harness.DeliverAsync((0, 0), (0, 1));
        await harness.DeliverEndOfPartitionAsync(0, 2);
        harness.Consumer.Resume([PipelineHarness.Partition0]);

        // E.g. the reader of a channel replaced by a restart
        using (PartitionChannel staleChannel = new(10, PipelineHarness.Partition0, harness))
        {
            await harness.Consumer.OnRecordReadAsync(staleChannel, PipelineHarness.Partition0, 1);
        }

        harness.CountPauses(PipelineHarness.Partition0).ShouldBe(1);
        harness.IsPartitionCaughtUp(0).ShouldBeFalse();

        // The current reader pauses the resumed partition again at its end
        gate.TrySetResult(true);
        await harness.WaitUntilCaughtUpAsync();
        await harness.Disconnected.Task.WaitAsync(Timeout);

        harness.CountPauses(PipelineHarness.Partition0).ShouldBe(2);
        harness.Errors.ShouldBeEmpty();
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The action is invoked before the consumer is disposed.")]
    public async Task WaitUntilCaughtUpAsync_ShouldThrow_WhenCatchUpIsNotEnabled()
    {
        IConfluentConsumerWrapper client = CreateSubstituteClient();
        using KafkaConsumer consumer = CreateSubstituteConsumer(client);

        Func<Task> act = () => consumer.WaitUntilCaughtUpAsync(TestContext.Current.CancellationToken);

        Exception exception = await act.ShouldThrowAsync<InvalidOperationException>();
        exception.Message.ShouldStartWith("The catch-up isn't enabled for this consumer.");
    }
}
