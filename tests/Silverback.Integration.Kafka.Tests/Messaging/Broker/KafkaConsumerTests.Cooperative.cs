// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Kafka;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker;

public partial class KafkaConsumerTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The callback and polling stop are awaited before the harness is disposed")]
    public async Task CooperativeRebalance_ShouldDropAndReplayRecordReturnedByCanceledPoll_WhenRetainedReaderKeepsRunning(bool autoCommit)
    {
        await using PollHarness harness = new(true, true, backpressure: 2, autoCommit: autoCommit);
        await harness.StartAsync(0, 1);

        PartitionChannel retained = harness.GetChannel(1)!;
        ProcessingGate active = harness.Block(1, 0);
        await harness.DeliverAsync((1, 0));
        await active.Started.Task.WaitAsync(Timeout);
        await harness.DeliverAsync((1, 1), (1, 2));

        Task pollingStopped = Task.CompletedTask;
        await harness.PollAsync(() =>
        {
            // Force a canceled poll while the retained reader keeps running. This state isn't reachable in production, where the
            // polling is only canceled together with every reader when stopping: the test only pins the drop semantics.
            pollingStopped = harness.StopPollingAsync();
            harness.Revoke(0);

            return harness.Record(1, 3);
        });

        // The returned record must not be buffered behind the retained reader's records: it is consumed again after a seek
        await pollingStopped.WaitAsync(Timeout);

        harness.GetChannel(1).ShouldBeSameAs(retained);
        harness.GetChannel(0).ShouldBeNull();
        harness.Client.Assignment.ShouldBe([new TopicPartition("topic", 1)]);
        retained.ReadCancellationToken.IsCancellationRequested.ShouldBeFalse();
        active.ReaderStopped.Task.IsCompleted.ShouldBeFalse();
        harness.Completed.ShouldBeEmpty();
        harness.CommittedOffset(1).ShouldBe(0);
        harness.Logs.ShouldContain(log => log.StartsWith("Dropping message (topic[1]@3)", StringComparison.Ordinal));

        active.Release.TrySetResult(true);
        await PollHarness.WaitUntilAsync(() => harness.Completed.Count == 3);
        await PollHarness.WaitUntilAsync(() => harness.StoredOffset(1) == 3);
        harness.Client.Commit();

        harness.CommittedOffset(1).ShouldBe(3);
        harness.Seeks.ShouldBeEmpty();

        // The restart seeks back to the dropped record
        await harness.Consumer.StopAsync().AsTask().WaitAsync(Timeout);
        await harness.Consumer.StartAsync();
        await harness.PollAsync(() => null);

        harness.Seeks.ShouldBe([new TopicPartitionOffset("topic", 1, 3)]);

        await harness.DeliverAsync((1, 3));
        await PollHarness.WaitUntilAsync(() => harness.StoredOffset(1) == 4);
        harness.Client.Commit();

        _output.WriteLine($"Retained partition order: {string.Join(", ", harness.Completed.Select(delivery => delivery.Offset))}");

        harness.Completed.ShouldBe([new Delivery(1, 0, 1), new Delivery(1, 1, 1), new Delivery(1, 2, 1), new Delivery(1, 3, 1)]);
        harness.CommittedOffset(1).ShouldBe(4);
        harness.UnsafeStores.ShouldBeEmpty();
        harness.UnsafeCommits.ShouldBeEmpty();
        harness.Errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The rebalance callback is awaited before the harness is disposed")]
    public async Task CooperativeRebalance_ShouldDrainRetainedHandlerBeforeReplay_WhenSharingOneChannel(bool autoCommit)
    {
        await using PollHarness harness = new(false, true, backpressure: 2, parallelism: 1, autoCommit: autoCommit);
        await harness.StartAsync(0, 1);
        await harness.DeliverAsync((1, 0));
        await PollHarness.WaitUntilAsync(() => harness.StoredOffset(1) == 1);
        harness.Client.Commit();

        PartitionChannel original = harness.GetChannel(1)!;
        ProcessingGate active = harness.Block(1, 1);
        active.IgnoreCancellation = true;
        await harness.DeliverAsync((1, 1), (1, 2), (1, 3));
        await active.Started.Task.WaitAsync(Timeout);

        Task rebalance = harness.PollAsync(() =>
        {
            harness.Revoke(0);

            return null;
        });

        await active.ReaderStopped.Task.WaitAsync(Timeout);

        rebalance.IsCompleted.ShouldBeFalse();
        harness.GetChannel(1).ShouldBeSameAs(original);
        harness.StoredOffset(1).ShouldBe(1, "The active retained record has not completed");

        active.Release.TrySetResult(true);
        await rebalance;

        PartitionChannel replacement = harness.GetChannel(1)!;
        KafkaOffset originalOffset = (KafkaOffset)active.Context!.Envelope.BrokerMessageIdentifier;

        replacement.ShouldNotBeSameAs(original);
        originalOffset.BelongsToChannel(replacement.InstanceId).ShouldBeFalse();
        harness.Client.Assignment.ShouldBe([new TopicPartition("topic", 1)]);
        harness.Seeks.ShouldBe([new TopicPartitionOffset("topic", 1, 1)]);
        harness.StoredOffset(1).ShouldBe(1, "Retiring retained work must replay instead of committing after rollback begins");

        harness.Client.ClearReceivedCalls();
        await harness.Consumer.CommitAsync(originalOffset);

        harness.Client.DidNotReceive().StoreOffset(Arg.Any<TopicPartitionOffset>());

        ProcessingGate replay = harness.Block(1, 1);
        await harness.DeliverAsync((1, 1), (1, 2), (1, 3));
        await replay.Started.Task.WaitAsync(Timeout);

        ((KafkaOffset)replay.Context!.Envelope.BrokerMessageIdentifier).BelongsToChannel(replacement.InstanceId).ShouldBeTrue();

        replay.Release.TrySetResult(true);
        await PollHarness.WaitUntilAsync(() => harness.StoredOffset(1) == 4);
        harness.Client.Commit();

        harness.Completed.Select(delivery => delivery.Offset).ShouldBe([0L, 1L, 1L, 2L, 3L]);
        harness.Completed.ShouldAllBe(delivery => delivery.Partition == 1 && delivery.Epoch == 1);
        harness.CommittedOffset(1).ShouldBe(4);
        harness.UnsafeStores.ShouldBeEmpty();
        harness.UnsafeCommits.ShouldBeEmpty();
        harness.Errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CooperativeRebalance_ShouldReplayRetainedPartialBatchWithoutAdvancingItsCommit(bool autoCommit)
    {
        await using PollHarness harness = new(false, true, backpressure: 2, parallelism: 1, autoCommit: autoCommit, batchPipeline: true);
        await harness.StartAsync(0, 1);
        await harness.DeliverAsync([.. Enumerable.Range(0, 10).Select(offset => (1, (long)offset))]);
        await PollHarness.WaitUntilAsync(() => harness.StoredOffset(1) == 10);
        harness.Client.Commit();

        PartitionChannel original = harness.GetChannel(1)!;
        await harness.DeliverAsync([.. Enumerable.Range(10, 5).Select(offset => (1, (long)offset))]);
        await PollHarness.WaitUntilAsync(() => harness.Completed.Count == 15);

        await harness.PollAsync(() =>
        {
            harness.Revoke(0);

            return null;
        });

        harness.GetChannel(1).ShouldNotBeSameAs(original);
        harness.Client.Assignment.ShouldBe([new TopicPartition("topic", 1)]);
        harness.Seeks.ShouldBe([new TopicPartitionOffset("topic", 1, 10)]);
        harness.StoredOffset(1).ShouldBe(10);
        harness.CommittedOffset(1).ShouldBe(10);
        harness.CompletedBatchSizes.ShouldBe([10]);
        harness.Completed.Select(delivery => delivery.Offset).ShouldBe(Enumerable.Range(0, 15).Select(offset => (long)offset));

        await harness.DeliverAsync([.. Enumerable.Range(10, 10).Select(offset => (1, (long)offset))]);
        await PollHarness.WaitUntilAsync(() => harness.StoredOffset(1) == 20);
        harness.Client.Commit();

        harness.Completed.Select(delivery => delivery.Offset).ShouldBe(Enumerable.Range(0, 15).Concat(Enumerable.Range(10, 10)).Select(offset => (long)offset));
        harness.Completed.Select(delivery => delivery.Offset).Distinct().ShouldBe(Enumerable.Range(0, 20).Select(offset => (long)offset));
        harness.Completed.ShouldAllBe(delivery => delivery.Partition == 1 && delivery.Epoch == 1);
        harness.CompletedBatchSizes.ShouldBe([10, 10]);
        harness.CommittedOffset(1).ShouldBe(20);
        harness.UnsafeStores.ShouldBeEmpty();
        harness.UnsafeCommits.ShouldBeEmpty();
        harness.Errors.ShouldBeEmpty();
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The rebalance callback is awaited before the harness is disposed")]
    public async Task CooperativeRebalance_ShouldResumeRetainedPartitionOnRestart_WhenStopDefeatsItsReplay()
    {
        await using PollHarness harness = new(false, true);
        await harness.StartAsync(0, 1);

        ProcessingGate active = harness.Block(1, 0);
        await harness.DeliverAsync((1, 0), (1, 1));
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

        // The retained partition is paused while the shared channel is rebuilt to replay it
        Task rebalance = harness.PollAsync(() =>
        {
            harness.Revoke(0);

            return null;
        });

        try
        {
            await entered.Task.WaitAsync(Timeout);

            // The polling is blocked in the revocation callback until the channel stops: the stop can't wait for it
            await harness.Consumer.StopAsync(false).AsTask().WaitAsync(Timeout);
        }
        finally
        {
            release.TrySetResult(true);
            active.Release.TrySetResult(true);
            await rebalance.WaitAsync(Timeout);
        }

        await harness.PollingStopped.WaitAsync(Timeout);

        harness.Resumes.ShouldBeEmpty();
        harness.Paused.ShouldBe([new TopicPartition("topic", 1)]);

        await harness.Consumer.StartAsync();
        await harness.PollAsync(() => null);

        harness.Seeks.ShouldBe([new TopicPartitionOffset("topic", 1, 0)]);
        harness.Resumes.ShouldBe([new TopicPartition("topic", 1)]);
        harness.Paused.ShouldBeEmpty();

        await harness.DeliverAsync((1, 0), (1, 1));
        await harness.WaitForCommitAsync(1, 2);

        harness.Completed.Select(delivery => delivery.Offset).ShouldBe([0L, 1L]);
        harness.UnsafeStores.ShouldBeEmpty();
        harness.UnsafeCommits.ShouldBeEmpty();
        harness.Errors.ShouldBeEmpty();
    }
}
